using System.Security.Claims;
using LibraryService.SharedKernel.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace LibraryService.Modules.Auth;

public sealed class UserStatusRequirement : IAuthorizationRequirement
{
}

public sealed class UserStatusAuthorizationHandler : AuthorizationHandler<UserStatusRequirement>
{
    public const string InactiveFailure = "inactive";
    public const string ExpiredFailure = "expired";

    private readonly IUserAuthStateStore authStateStore;
    private readonly UserManager<ApplicationUser> userManager;

    public UserStatusAuthorizationHandler(
        IUserAuthStateStore authStateStore,
        UserManager<ApplicationUser> userManager)
    {
        this.authStateStore = authStateStore;
        this.userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserStatusRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var userId = userManager.GetUserId(context.User);
        if (string.IsNullOrEmpty(userId))
        {
            context.Fail(new AuthorizationFailureReason(this, InactiveFailure));
            return;
        }

        var state = await authStateStore.GetAsync(userId);
        if (state is null || !state.IsActive)
        {
            context.Fail(new AuthorizationFailureReason(this, InactiveFailure));
            return;
        }

        var isL1 = state.Roles.Contains(AppRoles.SubscriptionL1)
            || context.User.IsInRole(AppRoles.SubscriptionL1);
        if (isL1 && state.SubscriptionExpirationDate.UtcDateTime < DateTime.UtcNow)
        {
            context.Fail(new AuthorizationFailureReason(this, ExpiredFailure));
            return;
        }

        var stampClaim = context.User.FindFirstValue(userManager.Options.ClaimsIdentity.SecurityStampClaimType);
        if (!string.Equals(stampClaim, state.SecurityStamp, StringComparison.Ordinal))
        {
            context.Fail(new AuthorizationFailureReason(this, InactiveFailure));
            return;
        }

        context.Succeed(requirement);
    }
}

public sealed class UserStatusResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            var reason = authorizeResult.AuthorizationFailure?.FailureReasons
                .Select(r => r.Message)
                .FirstOrDefault();

            if (reason == UserStatusAuthorizationHandler.InactiveFailure)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}

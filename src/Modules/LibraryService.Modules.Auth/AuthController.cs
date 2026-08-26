using System.Security.Claims;
using LibraryService.SharedKernel.Data;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LibraryService.Modules.Auth;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly IUserSessionService sessions;
    private readonly IUserAuthStateStore authStateStore;
    private readonly IOptionsMonitor<BearerTokenOptions> bearerTokenOptions;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserSessionService sessions,
        IUserAuthStateStore authStateStore,
        IOptionsMonitor<BearerTokenOptions> bearerTokenOptions)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.sessions = sessions;
        this.authStateStore = authStateStore;
        this.bearerTokenOptions = bearerTokenOptions;
    }

    [HttpPost("/register")]
    [AllowAnonymous]
    public Task<IActionResult> Register(EmailPasswordRequest request)
        => CreateUserAsync(request, AppRoles.SubscriptionL1);

    [HttpPost("/admin/register")]
    [AllowAnonymous]
    public async Task<IActionResult> AdminRegister(EmailPasswordRequest request)
    {
        var adminCount = (await userManager.GetUsersInRoleAsync(AppRoles.Admin)).Count;
        if (adminCount > 0)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized();
            if (!User.IsInRole(AppRoles.Admin))
                return Forbid();
        }

        return await CreateUserAsync(request, AppRoles.Admin);
    }

    [HttpPost("/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(EmailPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            return Unauthorized();

        var password = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!password.Succeeded)
            return Unauthorized();

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        return SignIn(principal, authenticationScheme: IdentityConstants.BearerScheme);
    }

    [HttpPost("/refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        var protector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var ticket = protector?.Unprotect(request.RefreshToken);
        if (ticket?.Properties.ExpiresUtc is not { } expiresUtc || DateTimeOffset.UtcNow >= expiresUtc)
            return Unauthorized();

        var userId = userManager.GetUserId(ticket.Principal);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var state = await authStateStore.GetAsync(userId);
        if (state is null || !state.IsActive)
            return Unauthorized();

        var stampClaim = ticket.Principal.FindFirstValue(userManager.Options.ClaimsIdentity.SecurityStampClaimType);
        if (!string.Equals(stampClaim, state.SecurityStamp, StringComparison.Ordinal))
            return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Unauthorized();

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        return SignIn(principal, authenticationScheme: IdentityConstants.BearerScheme);
    }

    [HttpPost("/logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        await sessions.RevokeAllAsync(user);
        return Ok();
    }

    private async Task<IActionResult> CreateUserAsync(EmailPasswordRequest request, string role)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest();

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            IsActive = true,
            SubscriptionExpirationDate = DateTimeOffset.UtcNow.AddYears(1)
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return BadRequest();

        await userManager.AddToRoleAsync(user, role);
        return Ok();
    }
}

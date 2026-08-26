using LibraryService.SharedKernel.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LibraryService.Modules.Auth;

[ApiController]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IUserSessionService sessions;
    private readonly IUserAuthStateStore authStateStore;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        IUserSessionService sessions,
        IUserAuthStateStore authStateStore)
    {
        this.userManager = userManager;
        this.sessions = sessions;
        this.authStateStore = authStateStore;
    }

    [HttpGet("/users/me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        return Ok(await ToDtoAsync(user));
    }

    [HttpGet("/users")]
    [Authorize]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> List()
    {
        var users = userManager.Users.ToList();
        var dtos = new List<UserDto>(users.Count);
        foreach (var user in users)
            dtos.Add(await ToDtoAsync(user));

        return Ok(dtos);
    }

    [HttpGet("/users/{id}")]
    [Authorize]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        return Ok(await ToDtoAsync(user));
    }

    [HttpPatch("/users/{id}/active")]
    [Authorize]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SetActive(string id, IsActiveRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        var callerId = userManager.GetUserId(User);
        if (!request.IsActive && string.Equals(callerId, user.Id, StringComparison.Ordinal))
            return BadRequest();

        if (!request.IsActive && user.IsActive && await IsLastActiveAdminAsync(user))
            return BadRequest();

        user.IsActive = request.IsActive;
        await userManager.UpdateAsync(user);
        authStateStore.Invalidate(user.Id);

        if (!request.IsActive)
            await sessions.RevokeAllAsync(user);

        return NoContent();
    }

    [HttpPatch("/users/{id}/subscription")]
    [Authorize]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SetSubscription(string id, SubscriptionRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        user.SubscriptionExpirationDate = request.SubscriptionExpirationDate;
        await userManager.UpdateAsync(user);
        authStateStore.Invalidate(user.Id);

        // Do not bump SecurityStamp here. Stamp revoke unauthenticates the access token (401);
        // expired L1 must still authenticate so the status policy can return 403.
        return NoContent();
    }

    private async Task<bool> IsLastActiveAdminAsync(ApplicationUser user)
    {
        if (!await userManager.IsInRoleAsync(user, AppRoles.Admin))
            return false;

        var admins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
        return admins.Count(a => a.IsActive) <= 1;
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;
        return new UserDto(user.Id, user.Email ?? string.Empty, role, user.IsActive, user.SubscriptionExpirationDate);
    }
}

using LibraryService.SharedKernel.Data;
using Microsoft.AspNetCore.Identity;

namespace LibraryService.Modules.Auth;

public interface IUserSessionService
{
    Task RevokeAllAsync(ApplicationUser user);
}

public sealed class UserSessionService : IUserSessionService
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IUserAuthStateStore authStateStore;

    public UserSessionService(UserManager<ApplicationUser> userManager, IUserAuthStateStore authStateStore)
    {
        this.userManager = userManager;
        this.authStateStore = authStateStore;
    }

    public async Task RevokeAllAsync(ApplicationUser user)
    {
        await userManager.UpdateSecurityStampAsync(user);
        authStateStore.Invalidate(user.Id);
    }
}

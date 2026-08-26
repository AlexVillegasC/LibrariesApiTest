using LibraryService.SharedKernel.Data;
using Microsoft.AspNetCore.Identity;

namespace LibraryService.Modules.Auth;

public static class IdentitySeed
{
    public static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { AppRoles.Admin, AppRoles.SubscriptionL1 })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

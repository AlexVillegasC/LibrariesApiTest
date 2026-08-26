using Microsoft.AspNetCore.Identity;

namespace LibraryService.SharedKernel.Data;

public class ApplicationUser : IdentityUser
{
    public bool IsActive { get; set; } = true;

    public DateTimeOffset SubscriptionExpirationDate { get; set; }
}

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string SubscriptionL1 = "Subscription_L1";
}

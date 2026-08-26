using LibraryService.SharedKernel.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Identity;

namespace LibraryService.Modules.Auth;

public sealed record UserAuthState(
    bool IsActive,
    DateTimeOffset SubscriptionExpirationDate,
    string SecurityStamp,
    IReadOnlyList<string> Roles);

public interface IUserAuthStateStore
{
    Task<UserAuthState?> GetAsync(string userId, CancellationToken cancellationToken = default);

    void Invalidate(string userId);
}

public sealed class UserAuthStateStore : IUserAuthStateStore
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);
    private readonly IMemoryCache cache;
    private readonly UserManager<ApplicationUser> userManager;

    public UserAuthStateStore(IMemoryCache cache, UserManager<ApplicationUser> userManager)
    {
        this.cache = cache;
        this.userManager = userManager;
    }

    public async Task<UserAuthState?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(Key(userId), out UserAuthState? cached) && cached is not null)
            return cached;

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return null;

        var roles = await userManager.GetRolesAsync(user);
        var state = new UserAuthState(
            user.IsActive,
            user.SubscriptionExpirationDate,
            user.SecurityStamp ?? string.Empty,
            roles.ToList());

        cache.Set(Key(userId), state, CacheDuration);
        return state;
    }

    public void Invalidate(string userId) => cache.Remove(Key(userId));

    private static string Key(string userId) => $"user-auth:{userId}";
}

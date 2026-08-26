namespace LibraryService.Modules.Auth;

public record EmailPasswordRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record IsActiveRequest(bool IsActive);

public record SubscriptionRequest(DateTimeOffset SubscriptionExpirationDate);

public record UserDto(
    string Id,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset SubscriptionExpirationDate);

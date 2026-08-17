using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryService.Modules.Auth;

public record TokenResponse(string token);

[ApiController]
public class AuthController : Controller
{
    private readonly IAuthenticationService authenticationService;
    private readonly JwtSettings jwtSettings;

    public AuthController(IAuthenticationService authenticationService, JwtSettings jwtSettings)
    {
        this.authenticationService = authenticationService;
        this.jwtSettings = jwtSettings;
    }

    [HttpPost("/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(User user)
    {
        var validUser = await authenticationService.AuthenticateAsync(user.Email, user.Password);
        if (validUser is null)
            return Unauthorized();

        var token = TokenGenerator.GenerateToken(validUser, jwtSettings);

        return Ok(new TokenResponse(token));
    }
}

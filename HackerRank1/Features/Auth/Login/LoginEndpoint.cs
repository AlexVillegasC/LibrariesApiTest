using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LibraryService.WebAPI.Shared.Auth;
using LibraryService.WebAPI.Shared.Endpoints;
using Microsoft.IdentityModel.Tokens;

namespace LibraryService.WebAPI.Features.Auth.Login;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string token);

public class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/login", Handle)
            .AllowAnonymous();
    }

    private static IResult Handle(LoginRequest request, JwtSettings jwtSettings)
    {
        if (request.Email != "admin" || request.Password != "1234")
            return Results.Unauthorized();

        var token = GenerateToken(userId: 1, request.Email, role: "admin", jwtSettings);
        return Results.Ok(new LoginResponse(token));
    }

    private static string GenerateToken(int userId, string email, string role, JwtSettings jwtSettings)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
        var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: cred);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

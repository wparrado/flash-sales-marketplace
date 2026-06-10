using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FlashSales.Api.Auth;

public sealed record JwtOptions(string Key, string Issuer, string Audience, int ExpiryMinutes = 60);

public sealed record DemoUser(Guid Id, string Username, string Password);

/// <summary>
/// Issues HS256 access tokens. Demo users are hardcoded — replacing them with
/// an identity provider only touches this adapter, not the checkout flow that
/// consumes the authenticated buyer id claim.
/// </summary>
public sealed class JwtTokenService(JwtOptions options, TimeProvider clock)
{
    private static readonly IReadOnlyList<DemoUser> DemoUsers =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "demo", "demo123"),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "admin", "admin123")
    ];

    public (string Token, DateTimeOffset ExpiresAt)? Authenticate(string username, string password)
    {
        var user = DemoUsers.FirstOrDefault(u =>
            u.Username == username && u.Password == password);
        if (user is null)
            return null;

        var expiresAt = clock.GetUtcNow().AddMinutes(options.ExpiryMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.Username)
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}

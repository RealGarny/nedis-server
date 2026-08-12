using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nedis.Server.Modules.Identity.Domain;

namespace Nedis.Server.Modules.Identity.Infrastructure.Jwt;

internal class UserTokenService(IOptions<JwtOptions> options) : IUserTokenService
{
    public string Issue(User user)
    {
        var opts = options.Value;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.SigningKey)),
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: opts.Issuer,
            audience: opts.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(opts.AccessTokenLifetimeMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
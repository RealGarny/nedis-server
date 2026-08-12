using Nedis.Server.Modules.Identity.Domain;

namespace Nedis.Server.Modules.Identity.Infrastructure.Jwt;

public interface IUserTokenService
{
    string Issue(User user);
}
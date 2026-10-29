
using Nedis.Server.Modules.Identity.Domain;
using Nedis.Server.Modules.Identity.Shared;

namespace Nedis.Server.Modules.Identity.Infrastructure.Tokens;

// Issues the access + refresh pair. Every method only stages changes in the
// unit of work; the caller saves them, together with its own changes.
// Sessions passed in must be loaded with User.
public interface ISessionIssuer
{
    AuthResponse StartSession(User user);

    AuthResponse Rotate(Session session);

    AuthResponse Reissue(Session session);
}
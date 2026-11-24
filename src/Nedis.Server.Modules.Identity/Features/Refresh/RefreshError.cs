namespace Nedis.Server.Modules.Identity.Features.Refresh;

public enum RefreshError
{
    // Unknown, forged, expired, revoked or stolen token. The session is over.
    InvalidToken,

    // A parallel exchange of the same token is in flight, or its result is not
    // available yet. Temporary: the client keeps its token and tries again.
    ExchangeInProgress,

    // An error that was raised either by the database
    // or the code working incorrectly
    Unexpected
}
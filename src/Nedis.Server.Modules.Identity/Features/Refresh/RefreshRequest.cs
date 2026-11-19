namespace Nedis.Server.Modules.Identity.Features.Refresh;

// Nullable and without validation attributes on purpose: a missing or malformed
// token must produce 401, not the 400 that validation would return. The client
// treats 400 as a temporary failure and would keep retrying a dead token.
public record RefreshRequest(string? RefreshToken);
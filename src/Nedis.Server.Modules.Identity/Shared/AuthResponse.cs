namespace Nedis.Server.Modules.Identity.Shared;

public record AuthResponse(string AccessToken, Guid UserId, string Login);
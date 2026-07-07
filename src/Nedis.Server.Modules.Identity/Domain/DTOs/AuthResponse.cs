namespace Nedis.Server.Modules.Identity.DTOs;

public record AuthResponse(string AccessToken, Guid UserId, string Login);
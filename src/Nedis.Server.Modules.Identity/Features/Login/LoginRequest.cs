using System.ComponentModel.DataAnnotations;

namespace Nedis.Server.Modules.Identity.Features.Login;

public record LoginRequest(
    [property: Required]
    [property: MinLength(3)]
    [property: MaxLength(32)]
    string Login,

    [property: Required]
    [property: MinLength(8)]
    string Password
);
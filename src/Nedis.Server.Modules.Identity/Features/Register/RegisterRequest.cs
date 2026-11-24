using System.ComponentModel.DataAnnotations;

namespace Nedis.Server.Modules.Identity.Features.Register;

public record RegisterRequest(
    [property: Required]
    [property: MinLength(3)]
    [property: MaxLength(32)]
    string Login,

    [property: Required]
    [property: MinLength(8)]
    string Password
);
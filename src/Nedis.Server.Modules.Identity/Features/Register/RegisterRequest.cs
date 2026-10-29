using System.ComponentModel.DataAnnotations;

namespace Nedis.Server.Modules.Identity.Features.Register;

public record RegisterRequest(
    [Required, MinLength(3), MaxLength(32)] string Login,
    [Required, MinLength(8)] string Password
);
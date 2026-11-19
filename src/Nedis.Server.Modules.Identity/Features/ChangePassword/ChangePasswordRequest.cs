using System.ComponentModel.DataAnnotations;

namespace Nedis.Server.Modules.Identity.Features.ChangePassword;

public record ChangePasswordRequest(
    [Required] string OldPassword,
    [Required, MinLength(8)] string NewPassword
);
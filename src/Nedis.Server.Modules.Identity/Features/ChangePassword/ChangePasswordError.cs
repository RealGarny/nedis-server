namespace Nedis.Server.Modules.Identity.Features.ChangePassword;

public enum ChangePasswordError
{
    // The token is valid but its user is gone.
    UserNotFound,

    // Account signs in through the master server only: there is no local password to change.
    NoLocalPassword,

    InvalidCurrentPassword,
}
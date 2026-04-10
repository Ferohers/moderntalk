using System.ComponentModel.DataAnnotations;

namespace Server.WebPortal.Models;

public class RegisterRequest
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 30 characters")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Password is required")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters")]
    public string Password { get; set; } = "";

    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = "";

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(256)]
    public string? Email { get; set; }
}

public class LoginRequest
{
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = "";
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "New password is required")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 128 characters")]
    public string NewPassword { get; set; } = "";

    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmNewPassword { get; set; } = "";
}

public class RefreshRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = "";
}

public class ChangeEmailRequest
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "New email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(256)]
    public string NewEmail { get; set; } = "";
}

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = "";
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Reset token is required")]
    public string ResetToken { get; set; } = "";

    [Required(ErrorMessage = "New password is required")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 128 characters")]
    public string NewPassword { get; set; } = "";

    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmNewPassword { get; set; } = "";
}

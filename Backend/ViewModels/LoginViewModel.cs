using System.ComponentModel.DataAnnotations;

namespace BiDE.ViewModels
{
    public class LoginViewModel
    {
        [RequiredNotBlank(ErrorMessage = "Email address is required.")]
        public string Email { get; set; } = string.Empty;

        [RequiredNotBlank(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;

        public string? Redirect { get; set; }
    }
}

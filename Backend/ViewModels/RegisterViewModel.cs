using System.ComponentModel.DataAnnotations;

namespace BiDE.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please select a valid role.")]
        [RegularExpression("^(Student|Instructor)$", ErrorMessage = "Please select a valid role.")]
        public string Role { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name must be 50 characters or fewer.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name must be 50 characters or fewer.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(100, ErrorMessage = "Email must be 100 characters or fewer.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(15, ErrorMessage = "Phone number must be 15 characters or fewer.")]
        public string Contact { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Suburb must be 100 characters or fewer.")]
        public string? Suburb { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a security question.")]
        [StringLength(200)]
        public string SecurityQuestion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide a security answer.")]
        [StringLength(255)]
        public string SecurityAnswer { get; set; } = string.Empty;
    }
}

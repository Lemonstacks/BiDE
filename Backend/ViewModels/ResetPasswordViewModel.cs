using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BiDE.ViewModels
{
    public class ResetPasswordViewModel
    {
        [RequiredNotBlank(ErrorMessage = "Email address is required.")]
        public string Email { get; set; } = string.Empty;

        // Display only: filled by the controller from the account, never bound from the form.
        [BindNever]
        public string? Question { get; set; }

        [RequiredNotBlank(ErrorMessage = "Please answer the security question.")]
        public string Answer { get; set; } = string.Empty;

        [RequiredNotBlank(ErrorMessage = "Please enter a new password.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [StringLength(255)]
        public string NewPassword { get; set; } = string.Empty;

        [RequiredNotBlank(ErrorMessage = "Please confirm your new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

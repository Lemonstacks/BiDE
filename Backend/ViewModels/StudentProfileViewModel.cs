using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BiDE.ViewModels
{
    public class StudentProfileViewModel
    {
        [RequiredNotBlank(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name must be 50 characters or fewer.")]
        public string FirstName { get; set; } = string.Empty;

        [RequiredNotBlank(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name must be 50 characters or fewer.")]
        public string LastName { get; set; } = string.Empty;

        [RequiredNotBlank(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(15, ErrorMessage = "Phone number must be 15 characters or fewer.")]
        public string Contact { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Suburb must be 100 characters or fewer.")]
        public string? Suburb { get; set; }

        // The uploaded file (form field "profilePicture").
        public IFormFile? ProfilePicture { get; set; }

        // Display-only values, filled from the stored account and never bound from the form.
        [BindNever] public string? Email { get; set; }
        [BindNever] public string? CurrentPictureUrl { get; set; }
        [BindNever] public string Initials { get; set; } = string.Empty;
    }
}

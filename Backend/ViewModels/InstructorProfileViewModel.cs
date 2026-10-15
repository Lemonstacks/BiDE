using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BiDE.Models;

namespace BiDE.ViewModels
{
    public class InstructorProfileViewModel
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

        [StringLength(200, ErrorMessage = "Certification must be 200 characters or fewer.")]
        public string? Certification { get; set; }

        // The uploaded file (form field "profilePicture").
        public IFormFile? ProfilePicture { get; set; }

        // Display-only values, filled from the stored account and never bound from the form.
        // Experience is a verified credential managed by an administrator, so it is read-only here.
        [BindNever] public string? Email { get; set; }
        [BindNever] public int Experience { get; set; }
        [BindNever] public InstructorStatus Status { get; set; }
        [BindNever] public string? RejectionReason { get; set; }
        [BindNever] public string? CurrentPictureUrl { get; set; }
        [BindNever] public string Initials { get; set; } = string.Empty;
    }
}

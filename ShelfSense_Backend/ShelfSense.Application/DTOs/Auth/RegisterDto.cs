using System;
using System.ComponentModel.DataAnnotations;

namespace ShelfSense.Application.DTOs.Auth
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Name must contain only letters and spaces.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Phone number must be a valid 10-digit Indian mobile number.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")] // Changed MinLength to 8 for better security
        // Relaxed regex slightly, but kept essential requirements
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must be at least 8 characters and contain lowercase, uppercase, digit, and special character.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; }

       
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace ShelfSense.Application.DTOs
{
    /// <summary>
    /// DTO for creating a new Staff member (POST request body).
    /// </summary>
    public class StaffCreateRequest
    {
        [Required(ErrorMessage = "Store ID is required.")]
        public long StoreId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        [RegularExpression("staff|manager")]
        public string Role { get; set; } = "staff";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "PasswordHash (plain text password) is required.")]
        // Note: The controller retrieves this plain text for hashing.
        public string PasswordHash { get; set; } = string.Empty;

        // Ensure other non-required properties from earlier requests are ignored or removed,
        // such as 'phone', as they are not on the DTO.
    }

    /// <summary>
    /// DTO for sending Staff data in a response.
    /// </summary>
    public class StaffResponse
    {
        public long StaffId { get; set; }
        public long StoreId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for partially updating a Staff member (PATCH request body).
    /// </summary>
    public class StaffUpdateRequest
    {
        public long? StoreId { get; set; }
        public string? Name { get; set; }
        public string? Role { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; } // New password if changing
    }
}
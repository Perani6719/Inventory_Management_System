using System.ComponentModel.DataAnnotations;

namespace ShelfSense.Application.DTOs
{
    /// <summary>
    /// DTO for the Admin to create a new Store Manager.
    /// Note: The role is fixed to 'manager' or explicitly set by the Admin.
    /// </summary>
    public class ManagerCreateRequest
    {
        [Required(ErrorMessage = "Store ID is required for a Manager.")]
        public long StoreId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string PasswordHash { get; set; } = string.Empty; // Plain text password

        
        public string Role { get; set; } = "manager";
    }
}
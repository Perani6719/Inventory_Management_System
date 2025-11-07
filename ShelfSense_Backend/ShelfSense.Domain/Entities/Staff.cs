using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Domain.Entities
{
    public class Staff
    {
        [Key]
        public long StaffId { get; set; }

        [Required]
        [ForeignKey(nameof(Store))]
        public long StoreId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [RegularExpression("staff|manager")]
        public string Role { get; set; } = "staff";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        // This stores the hashed password.
        public string PasswordHash { get; set; } = string.Empty;

        // 🔑 FIX: This property is now correctly managed in the controller.
        public DateTime CreatedAt { get; set; }

        // Navigation
        public Store? Store { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ShelfSense.Domain.Identity
{
    using Microsoft.AspNetCore.Identity;

    public class ApplicationUser : IdentityUser
    {
        // Custom properties for user profile
        public string Name { get; set; } = string.Empty;
        public string RoleType { get; set; } = string.Empty;

        // NEW: Property to track which store the user belongs to (required for manager seeding)
        public long? StoreId { get; set; }

        // Properties for JWT refresh token
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
    }


}

using Microsoft.AspNetCore.Identity;
using ShelfSense.Domain.Identity; // Assuming ApplicationUser is here
using System.Threading.Tasks;
using System.Linq; // Added for clarity, though not strictly required

namespace ShelfSense.Infrastructure.Seeders
{
    public static class UserAndRoleSeeder
    {
        public static async Task SeedRolesAndDefaultUserAsync(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            // --- 1. Role Seeding (Added 'admin') ---
            // 'admin' role is now the top-level user for adding managers and staff.
            string[] roles = { "admin", "manager", "staff", "warehouse" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // --- 2. Default System Admin User Seeding ---
            string adminEmail = "shelfsenseproject@gmail.com"; // Dedicated Admin email
            string adminPassword = "Admin@123";      // Secure default password

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Name = "Avinash",
                    PhoneNumber = "9391941521",
                    RoleType = "admin",
                    StoreId = null // Admin manages all, no specific store ID
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "admin");
                }
                // Optional: Console.WriteLine(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // --- 3. Default Store Manager User Seeding ---
            string managerEmail = "d.avinashkumar22@gmail.com";
            string managerPassword = "Avinash@123";

            if (await userManager.FindByEmailAsync(managerEmail) == null)
            {
                var managerUser = new ApplicationUser
                {
                    UserName = managerEmail,
                    Email = managerEmail,
                    Name = "Avinash",
                    PhoneNumber = "9391941521",
                    RoleType = "manager",
                    StoreId = 1L // SEEDING THE MANAGER FOR STORE 1
                };

                var result = await userManager.CreateAsync(managerUser, managerPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, "manager");
                }
            }

            // --- 4. Default Warehouse User Seeding ---
            string warehouseEmail = "gaddelakshmilavanya@gmail.com";
            string warehousePassword = "GLavanya@2002";

            if (await userManager.FindByEmailAsync(warehouseEmail) == null)
            {
                var warehouseUser = new ApplicationUser
                {
                    UserName = warehouseEmail,
                    Email = warehouseEmail,
                    Name = "Warehouse Admin",
                    PhoneNumber = "9391941521",
                    RoleType = "warehouse",
                    StoreId = null // Warehouse is generally not tied to a specific StoreId
                };

                var result = await userManager.CreateAsync(warehouseUser, warehousePassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(warehouseUser, "warehouse");
                }
            }
        }
    }
}
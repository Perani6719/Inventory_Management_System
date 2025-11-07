// SHELFSENSE.APPLICATION.SERVICES/AlertingService.cs

using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Identity; // Assuming ApplicationUser is here
using System.Collections.Generic;

namespace ShelfSense.Application.Services
{
    public class AlertingService
    {
        private readonly IProductShelfRepository _productShelfRepository;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        // ---------------- CONSTRUCTOR ----------------
        public AlertingService(
            IProductShelfRepository productShelfRepository,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager) // ADD USER MANAGER
        {
            _productShelfRepository = productShelfRepository;
            _emailService = emailService;
            _userManager = userManager;
        }

        // This method will be scheduled by Hangfire
        public async Task CheckAndNotifyAlerts()
        {
            // 1. RUN THE PREDICTION AND GET NEW ALERTS
            var newAlerts = await _productShelfRepository.RunPredictionAndGenerateAlertsAsync();

            if (newAlerts != null && newAlerts.Any())
            {
                // Group new alerts by the StoreId they belong to
                var alertsByStore = newAlerts
                    .GroupBy(a => a.Shelf.StoreId) // Requires Shelf navigation property
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var (storeId, storeAlerts) in alertsByStore)
                {
                    // 2. FIND THE MANAGER'S EMAIL FOR THIS SPECIFIC STORE
                    // Logic: Find the first user who is a 'manager' and is assigned to this store.
                    // IMPORTANT: We rely on the ApplicationUser having the StoreId property populated.
                    var manager = await _userManager.Users
                        .Where(u => u.StoreId == storeId)
                        .FirstOrDefaultAsync(); // Assuming one primary manager per store for alerts

                    if (manager == null || string.IsNullOrEmpty(manager.Email))
                    {
                        Console.WriteLine($"Alert: No manager email found for Store ID {storeId}. Skipping notification.");
                        continue;
                    }

                    var managerEmail = manager.Email;

                    // 3. GET ALL ACTIVE ALERTS FOR EMAIL CONTEXT (Filtered by Store)
                    var activeAlertsQuery = await _productShelfRepository.GetActiveAlertsAsync();
                    var activeStoreAlerts = await activeAlertsQuery
                        .Where(a => a.Shelf.StoreId == storeId)
                        .ToArrayAsync();

                    // 4. BUILD THE EMAIL BODY
                    var alertListHtml = activeStoreAlerts
                        .Take(10)
                        .Aggregate("", (current, a) =>
                            current + $"<li>{a.ProductShelf?.ProductName ?? "Unknown Product"} (Shelf: {a.Shelf?.ShelfCode ?? "Unknown Shelf"}) - Urgency: {a.UrgencyLevel}</li>");

                    var subject = $"CRITICAL: {storeAlerts.Count} NEW Replenishment Alert(s) for Store ID {storeId}";
                    var body = $"<h1>Dear Manager Avinash ({manager.UserName}),</h1>" +
                               $"<p>{storeAlerts.Count} new stock alert(s) were generated for your store, Store ID {storeId}, in the last run.</p>" +
                               $"<p>Total Active Alerts: {activeStoreAlerts.Count()} in your store.</p>" +
                               $"<p>Review the top 10 active alerts:</p>" +
                               $"<ul>{alertListHtml}</ul>" +
                               $"<p>Please log in to the dashboard to review and fulfill them.</p>";

                    // 5. SEND EMAIL
                    await _emailService.SendEmailAsync(managerEmail, subject, body);
                }
            }
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Infrastructure.Data; // Assuming DbContext is here
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShelfSense.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ShelfSenseDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(ShelfSenseDbContext context, ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<DashboardInventoryReportResponse>> GetInventorySummaryAsync()
        {
            try
            {
                // --- 1. Fetch Core Data (Current State) ---
                var productShelves = await _context.ProductShelves
                    // Select only the necessary fields to keep the query light
                    .Select(ps => new { ps.ProductId, ps.ShelfId, ps.Quantity })
                    .AsNoTracking()
                    .ToListAsync();

                // --- 2. Fetch Aggregated Task/Alert Data Efficiently ---

                // A. Get the LATEST Completed Restock Quantity for each Product/Shelf pair.
                var latestRestockDetails = await _context.RestockTasks
                    .Where(rt => rt.Status == "completed" && rt.CompletedAt.HasValue)
                    // Group by unique Product/Shelf pair
                    .GroupBy(rt => new { rt.ProductId, rt.ShelfId })
                    // For each group, select the task with the maximum (latest) CompletedAt date
                    .Select(g => g.OrderByDescending(rt => rt.CompletedAt).First())
                    // Project results to a dictionary for fast lookup
                    .ToDictionaryAsync(
                        k => (k.ProductId, k.ShelfId),
                        v => (int?)v.QuantityRestocked // Store the quantity
                    );


                // B. Get status of open ReplenishmentAlerts
                var openAlerts = await _context.ReplenishmentAlerts
                    .Where(ra => ra.Status != "closed")
                    .Select(ra => new { ra.ProductId, ra.ShelfId })
                    .AsNoTracking()
                    .ToDictionaryAsync(
                        k => (k.ProductId, k.ShelfId),
                        v => true // Use a simple boolean value
                    );

                // C. Get status of open RestockTasks (Status is not completed or cancelled)
                var openTasks = await _context.RestockTasks
                    .Where(rt => rt.Status != "completed" && rt.Status != "cancelled")
                    .Select(rt => new { rt.ProductId, rt.ShelfId })
                    .AsNoTracking()
                    .ToDictionaryAsync(
                        k => (k.ProductId, k.ShelfId),
                        v => true
                    );

                // D. Count open StockRequests (DeliveryStatus is not delivered or cancelled)
                var openStockRequests = await _context.StockRequests
                    .Where(sr => sr.DeliveryStatus != "delivered" && sr.DeliveryStatus != "cancelled")
                    .GroupBy(sr => sr.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        Count = g.Count()
                    })
                    .AsNoTracking()
                    .ToDictionaryAsync(
                        k => k.ProductId,
                        v => v.Count
                    );


                // --- 3. Combine Data into DTOs ---
                var response = new List<DashboardInventoryReportResponse>();

                foreach (var ps in productShelves)
                {
                    var key = (ps.ProductId, ps.ShelfId);

                    response.Add(new DashboardInventoryReportResponse
                    {
                        ProductId = ps.ProductId,
                        ShelfId = ps.ShelfId,
                        // Use the current server time for the snapshot date
                        ReportDate = DateTime.Today.Add(DateTime.Now.TimeOfDay),
                        QuantityOnShelf = ps.Quantity,

                        // Get restock quantity from dictionary (B)
                        LatestRestockQuantity = latestRestockDetails.GetValueOrDefault(key),

                        // Check existence in dictionaries (C, D, E)
                        AlertTriggered = openAlerts.ContainsKey(key),
                        OpenRestockTaskExists = openTasks.ContainsKey(key),
                        OpenStockRequestCount = openStockRequests.GetValueOrDefault(ps.ProductId)
                    });
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled error occurred in DashboardService while generating the summary.");
                throw; // Re-throw to be caught by the controller's try/catch
            }
        }
    }
}
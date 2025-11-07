using Xunit;
using Microsoft.EntityFrameworkCore;
using ShelfSense.Infrastructure.Data;
using ShelfSense.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShelfSense.Tests
{
    public class StockDepletionPredictionTests
    {
        private ShelfSenseDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ShelfSenseDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ShelfSenseDbContext(options);

            // Seed SalesHistories
            context.SalesHistories.AddRange(new[]
            {
                new SalesHistory { ProductId = 1, Quantity = 10, SaleTime = DateTime.Today.AddDays(-3) },
                new SalesHistory { ProductId = 1, Quantity = 5, SaleTime = DateTime.Today.AddDays(-2) },
                new SalesHistory { ProductId = 1, Quantity = 15, SaleTime = DateTime.Today.AddDays(-1) }
            });

            // Seed ProductShelves
            context.ProductShelves.Add(new ProductShelf
            {
                ProductId = 1,
                ShelfId = 101,
                Quantity = 20
            });

            context.SaveChanges();
            return context;
        }

        // ✅ Test 1: Predicting Depletion Date
        [Fact]
        public async Task PredictDepletion_ShouldCalculateExpectedDateCorrectly()
        {
            var context = GetDbContext();
            var today = DateTime.Today;

            var velocityData = await (
                from sale in context.SalesHistories
                group sale by new { sale.ProductId, SaleDay = sale.SaleTime.Date } into daily
                select new
                {
                    daily.Key.ProductId,
                    DailySales = daily.Sum(x => x.Quantity)
                }
            ).ToListAsync();

            var salesVelocity = velocityData
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    SalesVelocity = g.Average(x => x.DailySales)
                }).ToDictionary(x => x.ProductId, x => x.SalesVelocity);

            var shelf = await context.ProductShelves.FirstAsync();
            var velocity = salesVelocity[shelf.ProductId];
            var expectedDays = Math.Round(shelf.Quantity / velocity, 2);
            var expectedDate = today.AddDays(expectedDays);

            Assert.True(expectedDays < 6);
            Assert.Equal(expectedDate.Date, today.AddDays(expectedDays).Date);
        }

        // ✅ Test 2: Generating Alert When Low Stock
        [Fact]
        public async Task GenerateAlert_ShouldInsertAlert_WhenLowStockDetected()
        {
            var context = GetDbContext();
            var today = DateTime.Today;

            var velocityData = await (
                from sale in context.SalesHistories
                group sale by new { sale.ProductId, SaleDay = sale.SaleTime.Date } into daily
                select new
                {
                    daily.Key.ProductId,
                    DailySales = daily.Sum(x => x.Quantity)
                }
            ).ToListAsync();

            var salesVelocity = velocityData
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    SalesVelocity = g.Average(x => x.DailySales)
                }).ToDictionary(x => x.ProductId, x => x.SalesVelocity);

            var shelves = await context.ProductShelves.ToListAsync();
            var alertsToInsert = new List<ReplenishmentAlert>();

            foreach (var ps in shelves)
            {
                if (!salesVelocity.ContainsKey(ps.ProductId)) continue;

                var velocity = salesVelocity[ps.ProductId];
                double daysToDepletion = ps.Quantity / velocity;
                bool isLowStock = daysToDepletion < 6;

                if (isLowStock)
                {
                    alertsToInsert.Add(new ReplenishmentAlert
                    {
                        ProductId = ps.ProductId,
                        ShelfId = ps.ShelfId,
                        PredictedDepletionDate = today.AddDays(daysToDepletion),
                        UrgencyLevel = "high",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (alertsToInsert.Any())
            {
                await context.ReplenishmentAlerts.AddRangeAsync(alertsToInsert);
                await context.SaveChangesAsync();
            }

            var alerts = await context.ReplenishmentAlerts.ToListAsync();
            Assert.Single(alerts);
            Assert.Equal("high", alerts[0].UrgencyLevel);
        }
    }
}
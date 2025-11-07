using ShelfSense.Application.DTOs;
using ShelfSense.Application.Interfaces;
using ShelfSense.Domain.Entities;
using ShelfSense.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShelfSense.Infrastructure.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ShelfSenseDbContext _context;

        public AnalyticsService(ShelfSenseDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StockoutReportItem>> GenerateStockoutReportAsync(DateTime startDate, DateTime adjustedEndDate, DateTime originalEndDate)
        {
            var relevantTasks = await _context.RestockTasks
                .AsNoTracking()
                .Where(t => t.CompletedAt.HasValue &&
                            t.CompletedAt.Value >= startDate.ToUniversalTime() &&
                            t.CompletedAt.Value < adjustedEndDate.ToUniversalTime())
                .ToListAsync();

            if (!relevantTasks.Any())
                return Enumerable.Empty<StockoutReportItem>();

            var reportData = relevantTasks
                .GroupBy(t => new { t.ProductId, t.ShelfId })
                .Select(g =>
                {
                    // FIX: Included "completed" status (based on your sample data) in addition to 
                    // "delivered" and "in_transit" to ensure tasks are counted.
                    var completedTasks = g.Where(t =>
                        (t.Status == "delivered" || t.Status == "in_transit" || t.Status == "completed") &&
                        t.CompletedAt.HasValue
                    ).ToList();

                    int completedTaskCount = completedTasks.Count;

                    // If no relevant completed tasks, return a zeroed item.
                    if (completedTaskCount == 0)
                    {
                        return new StockoutReportItem
                        {
                            ProductId = g.Key.ProductId,
                            ProductName = $"Product {g.Key.ProductId}",
                            ShelfId = g.Key.ShelfId,
                            ShelfLocation = $"Aisle-{g.Key.ShelfId}",
                            StockoutCount = g.Count(),
                            AvgReplenishmentTimeInHours = 0,
                            AvgReplenishmentDelayInHours = 0,
                            ShelfAvailabilityPercentage = g.Key.ProductId % 2 == 0 ? 98.5 : 95.0
                        };
                    }

                    // Calculate the replenishment time (CompletedAt - AssignedAt) for each valid task
                    var replenishmentTimes = completedTasks
                        .Select(t => t.CompletedAt!.Value - t.AssignedAt)
                        .ToList();

                    // 1. Calculate Total Replenishment Time
                    var totalRestockingTime = replenishmentTimes.Sum(ts => ts.TotalHours);

                    // 2. Calculate Total Delay Time
                    // Sum the difference (TotalHours - 2) only for tasks where the time exceeded 2 hours.
                    var totalDelayTime = replenishmentTimes
                        .Where(ts => ts.TotalHours > 2)
                        .Sum(ts => ts.TotalHours - 2);


                    return new StockoutReportItem
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = $"Product {g.Key.ProductId}",
                        ShelfId = g.Key.ShelfId,
                        ShelfLocation = $"Aisle-{g.Key.ShelfId}",
                        StockoutCount = g.Count(), // Total stockouts in the group
                        // Average is Total Time / Number of completed tasks
                        AvgReplenishmentTimeInHours = totalRestockingTime / completedTaskCount,
                        // Average is Total Delay / Number of completed tasks
                        AvgReplenishmentDelayInHours = totalDelayTime / completedTaskCount,
                        ShelfAvailabilityPercentage = g.Key.ProductId % 2 == 0 ? 98.5 : 95.0
                    };
                })
                .OrderByDescending(r => r.StockoutCount)
                .ToList();

            var productIds = reportData.Select(r => r.ProductId).Distinct().ToList();
            var shelfIds = reportData.Select(r => r.ShelfId).Distinct().ToList();

            var productNames = await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId, p => p.ProductName);

            var shelfLocations = await _context.Shelves
                .Where(s => shelfIds.Contains(s.ShelfId))
                .ToDictionaryAsync(s => s.ShelfId, s => s.LocationDescription);

            foreach (var item in reportData)
            {
                if (productNames.TryGetValue(item.ProductId, out var name))
                    item.ProductName = name;

                if (shelfLocations.TryGetValue(item.ShelfId, out var location))
                    item.ShelfLocation = location;
            }

            return reportData;
        }

        public async Task<byte[]> GenerateStockoutReportPdfAsync(DateTime startDate, DateTime adjustedEndDate, DateTime originalEndDate)
        {
            var reportItems = await GenerateStockoutReportAsync(startDate, adjustedEndDate, originalEndDate);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Element(header =>
                    {
                        header
                            .Text($"Stockout Report ({startDate:yyyy-MM-dd} to {originalEndDate:yyyy-MM-dd})")
                            .SemiBold()
                            .FontSize(18)
                            .AlignCenter();
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Product");
                            header.Cell().Element(CellStyle).Text("Shelf");
                            header.Cell().Element(CellStyle).Text("Stockouts");
                            header.Cell().Element(CellStyle).Text("Avg Time (hrs)");
                            header.Cell().Element(CellStyle).Text("Delay (hrs)");
                            header.Cell().Element(CellStyle).Text("Availability (%)");

                            static IContainer CellStyle(IContainer container) =>
                                container
                                    .PaddingVertical(5)
                                    .Background(Colors.Grey.Lighten2)
                                    .BorderBottom(1)
                                    .AlignCenter();
                        });

                        foreach (var item in reportItems)
                        {
                            table.Cell().Element(CellContentStyle).Text(item.ProductName);
                            table.Cell().Element(CellContentStyle).Text(item.ShelfLocation);
                            table.Cell().Element(CellContentStyle).Text(item.StockoutCount.ToString());
                            table.Cell().Element(CellContentStyle).Text($"{item.AvgReplenishmentTimeInHours:F2}");
                            table.Cell().Element(CellContentStyle).Text($"{item.AvgReplenishmentDelayInHours:F2}");
                            table.Cell().Element(CellContentStyle).Text($"{item.ShelfAvailabilityPercentage:F2}");

                            static IContainer CellContentStyle(IContainer container) =>
                                container.PaddingVertical(3).AlignLeft();
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
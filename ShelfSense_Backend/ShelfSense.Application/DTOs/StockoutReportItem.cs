using System;

namespace ShelfSense.Application.DTOs
{
    public class StockoutReportItem
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public long ShelfId { get; set; }
        public string ShelfLocation { get; set; } = string.Empty;

        // Metrics from LLD 4.5.1
        public int StockoutCount { get; set; }
        public double AvgReplenishmentTimeInHours { get; set; }
        public double AvgReplenishmentDelayInHours { get; set; }
        public double ShelfAvailabilityPercentage { get; set; } // % of time in stock
    }
}
using System;
using System.ComponentModel.DataAnnotations;

namespace ShelfSense.Domain.Entities
{
    public class InventoryReport
    {
        [Key]
        public long ReportId { get; set; }

        public long ShelfId { get; set; }
        public long ProductId { get; set; }

        // Metrics to track stockouts and efficiency
        public int StockoutFrequency { get; set; } // Number of stockouts for this product/shelf
        public TimeSpan AverageReplenishmentDelay { get; set; } // Average delay (Task completed after expected time)
        public double AverageShelfAvailability { get; set; } // Percentage of time product was in stock

        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties (Assumed existence of Shelf and Product entities)
        public Shelf? Shelf { get; set; }
        public Product? Product { get; set; }
    }
}
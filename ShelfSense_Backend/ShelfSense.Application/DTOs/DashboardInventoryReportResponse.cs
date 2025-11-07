using System;

namespace ShelfSense.Application.DTOs
{
    // DTO for the Dashboard's Inventory Summary table
    public class DashboardInventoryReportResponse
    {
        public long ProductId { get; set; }
        public long ShelfId { get; set; }
        public DateTime ReportDate { get; set; } // Current date/snapshot date

        // Current Inventory
        public int QuantityOnShelf { get; set; }

        // Restock Task Information
        // The quantity from the latest completed restock task
        public int? LatestRestockQuantity { get; set; }

        // Status Flags
        public bool AlertTriggered { get; set; } // Is there an open/pending alert?

        // NEW: Show if an open RestockTask exists for this Product/Shelf
        public bool OpenRestockTaskExists { get; set; }

        // NEW: Show if an open StockRequest exists for this Product
        public int OpenStockRequestCount { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Application.DTOs
{
    public class ReplenishmentAlertResponse
    {
        public long AlertId { get; set; }
        public long ProductId { get; set; }
        public long ShelfId { get; set; }
        public string? ProductName { get; set; } // Added for display
        public string? ShelfCode { get; set; } // Added for display
        public DateTime PredictedDepletionDate { get; set; }
        public string UrgencyLevel { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
         
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Application.DTOs
{
    public class ShelfMetric
    {
        public long ShelfId { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public int TotalCapacity { get; set; }
        public int CurrentStock { get; set; }
        public double OccupancyPercentage { get; set; }

        // Tracking Restocking Frequency
        public int TotalProductsAssigned { get; set; }
        public int RestockCountLast30Days { get; set; }
        public double AverageDaysBetweenRestocks { get; set; }
    }

}


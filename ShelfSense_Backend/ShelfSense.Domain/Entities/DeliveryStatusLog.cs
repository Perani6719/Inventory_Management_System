using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ShelfSense.Domain.Entities
{
    public class DeliveryStatusLog
    {
        public long DeliveryStatusLogId { get; set; } // Primary Key

        public long RequestId { get; set; }
        public long? AlertId { get; set; }

        public string DeliveryStatus { get; set; } = string.Empty;
        public DateTime StatusChangedAt { get; set; }

        // Navigation (optional)

        [JsonIgnore]
        public StockRequest? StockRequest { get; set; }
    }
}

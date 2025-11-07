using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfSense.Domain.Entities
{
    public class DashBoard
    {
        [Key]
        public long ProductId { get; set; }
        public long ShelfId { get; set; }
        public DateTime ReportDate { get; set; }
        public int QuantityOnShelf { get; set; }
        public int? QuantityRestocked { get; set; }
        public bool AlertTriggered { get; set; }
    }
}

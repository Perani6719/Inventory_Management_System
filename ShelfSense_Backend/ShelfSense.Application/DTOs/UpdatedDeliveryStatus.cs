using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShelfSense.Domain.Entities;

namespace ShelfSense.Application.DTOs
{
    public class UpdateDeliveryStatus
    {
        [Required]


        public long RequestId { get; set; }

        [Required]
        [RegularExpression("^(requested|in_transit|delivered|cancelled)$")]
        public string DeliveryStatus { get; set; }
    }
}


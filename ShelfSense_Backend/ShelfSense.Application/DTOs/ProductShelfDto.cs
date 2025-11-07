using System;
using System.ComponentModel.DataAnnotations;

namespace ShelfSense.Application.DTOs
{
    public class ProductShelfCreateRequest
    {
        [Required]
        public long ProductId { get; set; }

        [Required]
        public long ShelfId { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, int.MaxValue)]
        public int MaxCapacity { get; set; } = 200; // ✅ New field
    }


    // NEW DTO FOR AUTOMATION
    public class ProductShelfAutoAssignRequest
    {
        [Required]
        public long ProductId { get; set; }

        [Required]
        public long CategoryId { get; set; }

        [Range(0, int.MaxValue)]
        public int InitialQuantity { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int MaxCapacity { get; set; } = 200; // ✅ New field
    }


    public class ProductShelfResponse
    {
        public long ProductShelfId { get; set; }
        public long ProductId { get; set; }
        public long ShelfId { get; set; }
        public int Quantity { get; set; }
        public int MaxCapacity { get; set; } // ✅ New field
        public DateTime LastRestockedAt { get; set; }
    }

}
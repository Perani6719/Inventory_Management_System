using ShelfSense.Domain.Entities;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class RestockTask

{
    
    public long TaskId { get; set; }


    public long? AlertId { get; set; } // Make nullable

    [Required]

    public long ProductId { get; set; }

    [Required]

    public long ShelfId { get; set; }

    [Required]

    public long AssignedTo { get; set; }

    [Required]

    public string Status { get; set; } = "pending";//pending,completed ,delayed.

    public DateTime AssignedAt { get; set; }

    [Required]
    public int QuantityRestocked { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Navigation
    [JsonIgnore]

    public ReplenishmentAlert? Alert { get; set; }
    [JsonIgnore]

    public Product? Product { get; set; }
    [JsonIgnore]

    public Shelf? Shelf { get; set; }
    [JsonIgnore]
    public Staff? Staff { get; set; }

}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MultiAreaPortal.Models;

// Managed via Entity Framework Core in the Admin area (full CRUD).
public class Event
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Ticket price must be a non-negative value")]
    public decimal TicketPrice { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime EventDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

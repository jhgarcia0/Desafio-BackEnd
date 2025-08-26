namespace Rental.Api.Models;

public class Moto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Identifier { get; set; } = default!;
    public int Year { get; set; }
    public string Model { get; set; } = default!;
    public string Plate { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

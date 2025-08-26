namespace Rental.Api.Models;

public class Courier
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Identifier { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Cnpj { get; set; } = default!;
    public DateTime BirthDate { get; set; }
    public string CnhNumber { get; set; } = default!;
    public string CnhType { get; set; } = default!;
    public string? CnhImagePath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

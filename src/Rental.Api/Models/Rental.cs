namespace Rental.Api.Models;

public enum RentalStatus { Active = 1, Closed = 2 }

public sealed class Rental
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CourierId { get; set; }
    public Guid MotoId { get; set; }
    public int PlanDays { get; set; }
    public decimal DailyRate { get; set; }
    public DateTime StartDate { get; set; } 
    public DateTime ExpectedEndDate { get; set; }
    public DateTime? EndDate { get; set; }       
    public RentalStatus Status { get; set; } = RentalStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

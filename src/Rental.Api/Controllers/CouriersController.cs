using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rental.Api.Data;
using Rental.Api.Models;

namespace Rental.Api.Controllers;

[ApiController]
[Route("couriers")]
public class CouriersController : ControllerBase
{
    private readonly RentalDbContext _db;
    public CouriersController(RentalDbContext db) => _db = db;

    public record CreateCourierRequest(
        string Identifier,
        string Name,
        string Cnpj,
        DateTime BirthDate,
        string CnhNumber,
        string CnhType,
        string? CnhImagePath
    );

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCourierRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Identifier) ||
            string.IsNullOrWhiteSpace(req.Name) ||
            string.IsNullOrWhiteSpace(req.Cnpj) ||
            string.IsNullOrWhiteSpace(req.CnhNumber) ||
            string.IsNullOrWhiteSpace(req.CnhType))
            return BadRequest(new { error = "Identifier, Name, Cnpj, CnhNumber and CnhType are required." });

        var cnhType = req.CnhType.Trim().ToUpperInvariant();
        if (cnhType != "A" && cnhType != "B" && cnhType != "A+B")
            return BadRequest(new { error = "CnhType must be 'A', 'B' or 'A+B'." });

        var cnpj = req.Cnpj.Trim();
        var cnhNumber = req.CnhNumber.Trim();

        if (await _db.Couriers.AsNoTracking().AnyAsync(c => c.Cnpj == cnpj, ct))
            return Conflict(new { error = "Cnpj already exists." });

        if (await _db.Couriers.AsNoTracking().AnyAsync(c => c.CnhNumber == cnhNumber, ct))
            return Conflict(new { error = "CnhNumber already exists." });

        var entity = new Courier
        {
            Identifier = req.Identifier.Trim(),
            Name = req.Name.Trim(),
            Cnpj = cnpj,
            BirthDate = req.BirthDate,
            CnhNumber = cnhNumber,
            CnhType = cnhType,
            CnhImagePath = string.IsNullOrWhiteSpace(req.CnhImagePath) ? null : req.CnhImagePath.Trim()
        };

        _db.Couriers.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var courier = await _db.Couriers.FindAsync([id], ct);
        return courier is null ? NotFound() : Ok(courier);
    }
}
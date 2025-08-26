using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rental.Api.Data;
using Rental.Api.Models;

namespace Rental.Api.Controllers;

[ApiController]
[Route("motos")]
public class MotosController : ControllerBase
{
    private readonly RentalDbContext _db;
    public MotosController(RentalDbContext db) => _db = db;

    public record CreateMotoRequest(string Identifier, int Year, string Model, string Plate);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMotoRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Identifier) ||
            string.IsNullOrWhiteSpace(req.Model) ||
            string.IsNullOrWhiteSpace(req.Plate))
        {
            return BadRequest(new { error = "Identifier, Model and Plate are required." });
        }


        var plate = req.Plate.Trim().ToUpperInvariant();
        var exists = await _db.Motos.AsNoTracking().AnyAsync(m => m.Plate == plate, ct);
        if (exists) return Conflict(new { error = "Plate already exists." });

        var moto = new Moto
        {
            Identifier = req.Identifier.Trim(),
            Year = req.Year,
            Model = req.Model.Trim(),
            Plate = plate
        };

        _db.Motos.Add(moto);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = moto.Id }, moto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var moto = await _db.Motos.FindAsync([id], ct);
        return moto is null ? NotFound() : Ok(moto);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? plate, CancellationToken ct)
    {
        var query = _db.Motos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(plate))
        {
            var normalized = plate.Trim().ToUpperInvariant();
            query = query.Where(m => m.Plate == normalized);
        }

        var list = await query
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

        return Ok(list);
    }
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var moto = await _db.Motos.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (moto is null) return NotFound();

        _db.Motos.Remove(moto);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}

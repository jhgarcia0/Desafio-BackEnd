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

        var birthUtc = req.BirthDate.Kind == DateTimeKind.Utc
            ? req.BirthDate
            : DateTime.SpecifyKind(req.BirthDate, DateTimeKind.Utc);

        var entity = new Courier
        {
            Identifier = req.Identifier.Trim(),
            Name = req.Name.Trim(),
            Cnpj = cnpj,
            BirthDate = birthUtc,
            CnhNumber = cnhNumber,
            CnhType = cnhType,
            CnhImagePath = string.IsNullOrWhiteSpace(req.CnhImagePath) ? null : req.CnhImagePath.Trim()
        };

        try
        {
            _db.Couriers.Add(entity);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true)
        {
            return Conflict(new { error = "Unique constraint violation." });
        }

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPost("{id:guid}/cnh")]
    [RequestSizeLimit(10_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadCnh(Guid id, [FromForm] UploadCnhRequest req, CancellationToken ct)
    {
        var file = req.File;
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "file is required" });

        var allowed = new[] { "image/png", "image/bmp" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { error = "only PNG or BMP are allowed" });

        var courier = await _db.Couriers.FindAsync([id], ct);
        if (courier is null) return NotFound();

        var ext = file.ContentType == "image/png" ? ".png" : ".bmp";
        var baseDir = Path.Combine(AppContext.BaseDirectory, "storage", "cnh");
        Directory.CreateDirectory(baseDir);

        var fileName = $"{id}{ext}";
        var fullPath = Path.Combine(baseDir, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream, ct);

        courier.CnhImagePath = fullPath;
        await _db.SaveChangesAsync(ct);

        var relative = fullPath.Replace(AppContext.BaseDirectory, "").TrimStart(Path.DirectorySeparatorChar);
        return Ok(new { path = relative });
    }



    [HttpGet("{id:guid}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var courier = await _db.Couriers.FindAsync([id], ct);
        return courier is null ? NotFound() : Ok(courier);
    }
}
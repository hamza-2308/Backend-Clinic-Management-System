using ClinicApi.Data;
using ClinicApi.DTOs;
using ClinicApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiseasesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DiseasesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/diseases — public
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var diseases = await _db.Diseases
            .Include(d => d.Treatments)
            .Select(d => ToDto(d))
            .ToListAsync();

        return Ok(diseases);
    }

    // GET /api/diseases/{id} — public
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var disease = await _db.Diseases.Include(d => d.Treatments)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (disease == null) return NotFound(new { message = "Disease not found." });
        return Ok(ToDto(disease));
    }

    // POST /api/diseases — Admin only
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(DiseaseUpsertDto dto)
    {
        var disease = new Disease
        {
            Name = dto.Name,
            Description = dto.Description,
            Symptoms = dto.Symptoms,
            Causes = dto.Causes,
        };

        _db.Diseases.Add(disease);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(dto.TreatmentProcedure) || !string.IsNullOrWhiteSpace(dto.RecoveryTime))
        {
            _db.Treatments.Add(new Treatment
            {
                DiseaseId = disease.Id,
                Procedure = dto.TreatmentProcedure ?? string.Empty,
                EstimatedRecoveryTime = dto.RecoveryTime,
            });
            await _db.SaveChangesAsync();
        }

        var created = await _db.Diseases.Include(d => d.Treatments).FirstAsync(d => d.Id == disease.Id);
        return CreatedAtAction(nameof(GetById), new { id = disease.Id }, ToDto(created));
    }

    // PUT /api/diseases/{id} — Admin only
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, DiseaseUpsertDto dto)
    {
        var disease = await _db.Diseases.Include(d => d.Treatments).FirstOrDefaultAsync(d => d.Id == id);
        if (disease == null) return NotFound(new { message = "Disease not found." });

        disease.Name = dto.Name;
        disease.Description = dto.Description;
        disease.Symptoms = dto.Symptoms;
        disease.Causes = dto.Causes;

        var treatment = disease.Treatments.FirstOrDefault();
        if (treatment == null)
        {
            treatment = new Treatment { DiseaseId = disease.Id };
            _db.Treatments.Add(treatment);
        }
        treatment.Procedure = dto.TreatmentProcedure ?? string.Empty;
        treatment.EstimatedRecoveryTime = dto.RecoveryTime;

        await _db.SaveChangesAsync();
        return Ok(ToDto(disease));
    }

    // DELETE /api/diseases/{id} — Admin only
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var disease = await _db.Diseases.FindAsync(id);
        if (disease == null) return NotFound(new { message = "Disease not found." });

        _db.Diseases.Remove(disease); // cascades to Treatments
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static DiseaseDto ToDto(Disease d)
    {
        var primaryTreatment = d.Treatments.FirstOrDefault();
        return new DiseaseDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            Symptoms = d.Symptoms,
            Causes = d.Causes,
            TreatmentProcedure = primaryTreatment?.Procedure,
            RecoveryTime = primaryTreatment?.EstimatedRecoveryTime,
        };
    }
}

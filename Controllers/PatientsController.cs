using System.Security.Claims;
using ClinicApi.Data;
using ClinicApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PatientsController(ApplicationDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /api/patients — Admin only
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var patients = await _db.Patients
            .Select(p => new PatientDto { Id = p.Id, FullName = p.FullName, Email = p.Email, Phone = p.Phone })
            .ToListAsync();

        return Ok(patients);
    }

    // GET /api/patients/me — current patient's own profile
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == CurrentUserId);
        if (patient == null) return NotFound(new { message = "Patient profile not found." });

        return Ok(new PatientDto { Id = patient.Id, FullName = patient.FullName, Email = patient.Email, Phone = patient.Phone });
    }

    // PUT /api/patients/me — update own profile
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(PatientDto dto)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == CurrentUserId);
        if (patient == null) return NotFound(new { message = "Patient profile not found." });

        patient.FullName = dto.FullName;
        patient.Phone = dto.Phone;
        await _db.SaveChangesAsync();

        return Ok(dto);
    }

    // DELETE /api/patients/{id} — Admin only
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient == null) return NotFound(new { message = "Patient not found." });

        _db.Patients.Remove(patient);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

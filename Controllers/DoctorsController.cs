using ClinicApi.Data;
using ClinicApi.DTOs;
using ClinicApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DoctorsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/doctors — public
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var doctors = await _db.Doctors
            .Where(d => d.IsActive)
            .Select(d => ToDto(d))
            .ToListAsync();

        return Ok(doctors);
    }

    // GET /api/doctors/{id} — public
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var doctor = await _db.Doctors.FindAsync(id);
        if (doctor == null) return NotFound(new { message = "Doctor not found." });
        return Ok(ToDto(doctor));
    }

    // GET /api/doctors/{id}/availability — public
    // Returns a simple list of display time-slot strings for the doctor.
    [HttpGet("{id}/availability")]
    public async Task<IActionResult> GetAvailability(int id)
    {
        var slots = await _db.DoctorAvailabilities
            .Where(a => a.DoctorId == id)
            .OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime)
            .Select(a => new
            {
                a.DayOfWeek,
                StartTime = a.StartTime.ToString(@"hh\:mm"),
                EndTime = a.EndTime.ToString(@"hh\:mm"),
                a.SlotDurationMinutes
            })
            .ToListAsync();

        return Ok(slots);
    }

    // POST /api/doctors — Admin only
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromForm] DoctorUpsertDto dto)
    {
        string? photoPath = null;

        if (dto.Photo != null)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "doctors");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Photo.FileName);

            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Photo.CopyToAsync(stream);
            }

            photoPath = "/uploads/doctors/" + fileName;
        }

        var doctor = new Doctor
        {
            Name = dto.Name,
            Qualification = dto.Qualification,
            Specialization = dto.Specialization,
            ExperienceYears = dto.ExperienceYears,
            PhotoUrl = photoPath,
            AvailableDays = dto.AvailableDays,
            AvailableTimeSlots = dto.AvailableTimeSlots,
        };

        _db.Doctors.Add(doctor);

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = doctor.Id }, ToDto(doctor));
    }

    // PUT /api/doctors/{id} — Admin only
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromForm] DoctorUpsertDto dto)
    {
        var doctor = await _db.Doctors.FindAsync(id);

        if (doctor == null)
            return NotFound(new { message = "Doctor not found." });

        doctor.Name = dto.Name;
        doctor.Qualification = dto.Qualification;
        doctor.Specialization = dto.Specialization;
        doctor.ExperienceYears = dto.ExperienceYears;
        doctor.AvailableDays = dto.AvailableDays;
        doctor.AvailableTimeSlots = dto.AvailableTimeSlots;

        if (dto.Photo != null)
        {
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "doctors");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = Guid.NewGuid().ToString() +
                           Path.GetExtension(dto.Photo.FileName);

            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Photo.CopyToAsync(stream);
            }

            doctor.PhotoUrl = "/uploads/doctors/" + fileName;
        }

        await _db.SaveChangesAsync();

        return Ok(ToDto(doctor));
    }

    // DELETE /api/doctors/{id} — Admin only (soft delete)
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var doctor = await _db.Doctors.FindAsync(id);
        if (doctor == null) return NotFound(new { message = "Doctor not found." });

        doctor.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static DoctorDto ToDto(Doctor d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Qualification = d.Qualification,
        Specialization = d.Specialization,
        ExperienceYears = d.ExperienceYears,
        PhotoUrl = d.PhotoUrl,
        AvailableDays = d.AvailableDays,
        AvailableTimeSlots = d.AvailableTimeSlots,
    };
}

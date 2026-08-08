using System.Security.Claims;
using ClinicApi.Data;
using ClinicApi.DTOs;
using ClinicApi.Helpers;
using ClinicApi.Models;
using ClinicApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(ApplicationDbContext db, IAppointmentService appointmentService)
    {
        _db = db;
        _appointmentService = appointmentService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // POST /api/appointments — authenticated patients only
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Book(BookAppointmentDto dto)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == CurrentUserId);
        if (patient == null) return BadRequest(new { message = "No patient profile found for this account." });

        var doctor = await _db.Doctors.FindAsync(dto.DoctorId);
        if (doctor == null || !doctor.IsActive) return NotFound(new { message = "Doctor not found." });

        TimeSpan timeSlot;
        try
        {
            timeSlot = TimeSlotHelper.ParseTimeSlot(dto.TimeSlot);
        }
        catch (FormatException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        if (dto.Date < DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new { message = "Cannot book an appointment in the past." });

        try
        {
            var appointment = await _appointmentService.BookAppointmentAsync(
                patient.Id, dto.DoctorId, dto.Date, timeSlot, dto.Reason);

            return Ok(ToDto(appointment, patient.FullName, doctor.Name));
        }
        catch (DuplicateSlotException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // GET /api/appointments/my — authenticated patient's own appointments
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMine()
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == CurrentUserId);
        if (patient == null) return Ok(Array.Empty<AppointmentDto>());

        var appointments = await _db.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Include(a => a.Token)
            .Where(a => a.PatientId == patient.Id)
            .OrderByDescending(a => a.Date)
            .ToListAsync();

        return Ok(appointments.Select(a => ToDto(a, a.Patient!.FullName, a.Doctor!.Name)));
    }

    // GET /api/appointments — Admin only, with optional search/status filter
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status)
    {
        var query = _db.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Include(a => a.Token)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a =>
                a.Patient!.FullName.Contains(search) ||
                a.Doctor!.Name.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AppointmentStatus>(status, true, out var statusEnum))
        {
            query = query.Where(a => a.Status == statusEnum);
        }

        var appointments = await query.OrderByDescending(a => a.Date).ToListAsync();
        return Ok(appointments.Select(a => ToDto(a, a.Patient!.FullName, a.Doctor!.Name)));
    }

    // PUT /api/appointments/{id}/confirm — Admin only
    [HttpPut("{id}/confirm")]
    [Authorize(Roles = "Admin")]
    public Task<IActionResult> Confirm(int id) => ChangeStatus(id, AppointmentStatus.Confirmed);

    // PUT /api/appointments/{id}/complete — Admin only
    [HttpPut("{id}/complete")]
    [Authorize(Roles = "Admin")]
    public Task<IActionResult> Complete(int id) => ChangeStatus(id, AppointmentStatus.Completed);

    // PUT /api/appointments/{id}/cancel — Admin or the owning patient
    [HttpPut("{id}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(int id)
    {
        var appointment = await _db.Appointments.Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null) return NotFound(new { message = "Appointment not found." });

        var isAdmin = User.IsInRole("Admin");
        var isOwner = appointment.Patient?.UserId == CurrentUserId;
        if (!isAdmin && !isOwner) return Forbid();

        appointment.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // PUT /api/appointments/{id}/reschedule — Admin only
    [HttpPut("{id}/reschedule")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reschedule(int id, RescheduleDto dto)
    {
        var appointment = await _db.Appointments.Include(a => a.Doctor).Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null) return NotFound(new { message = "Appointment not found." });

        TimeSpan timeSlot;
        try
        {
            timeSlot = TimeSlotHelper.ParseTimeSlot(dto.TimeSlot);
        }
        catch (FormatException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var conflict = await _db.Appointments.AnyAsync(a =>
            a.Id != id &&
            a.DoctorId == appointment.DoctorId &&
            a.Date == dto.Date &&
            a.TimeSlot == timeSlot &&
            a.Status != AppointmentStatus.Cancelled);

        if (conflict) return Conflict(new { message = "That slot is already taken." });

        appointment.Date = dto.Date;
        appointment.TimeSlot = timeSlot;
        await _db.SaveChangesAsync();

        return Ok(ToDto(appointment, appointment.Patient!.FullName, appointment.Doctor!.Name));
    }

    private async Task<IActionResult> ChangeStatus(int id, AppointmentStatus status)
    {
        var appointment = await _db.Appointments.Include(a => a.Doctor).Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null) return NotFound(new { message = "Appointment not found." });

        appointment.Status = status;
        await _db.SaveChangesAsync();

        return Ok(ToDto(appointment, appointment.Patient!.FullName, appointment.Doctor!.Name));
    }

    private static AppointmentDto ToDto(Appointment a, string patientName, string doctorName) => new()
    {
        Id = a.Id,
        PatientName = patientName,
        DoctorName = doctorName,
        Date = a.Date.ToString("yyyy-MM-dd"),
        TimeSlot = TimeSlotHelper.FormatTimeSlot(a.TimeSlot),
        QueueNumber = a.QueueNumber,
        TokenNumber = a.Token?.TokenNumber ?? "",
        Status = a.Status.ToString(),
    };
}

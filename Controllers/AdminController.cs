using ClinicApi.Data;
using ClinicApi.DTOs;
using ClinicApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/admin/dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var stats = new DashboardStatsDto
        {
            TotalDoctors = await _db.Doctors.CountAsync(d => d.IsActive),
            TotalPatients = await _db.Patients.CountAsync(),
            TotalAppointments = await _db.Appointments.CountAsync(),
            TodaysAppointments = await _db.Appointments.CountAsync(a => a.Date == today),
            PendingAppointments = await _db.Appointments.CountAsync(a => a.Status == AppointmentStatus.Pending),
        };

        return Ok(stats);
    }
}

using ClinicApi.Data;
using ClinicApi.DTOs;
using ClinicApi.Models;
using ClinicApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthController(ApplicationDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "An account with this email already exists." });

        var patientRole = await _db.Roles.FirstAsync(r => r.Name == "Patient");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = patientRole.Id,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Every registered user gets a linked Patient profile automatically
        var patient = new Patient
        {
            UserId = user.Id,
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
        };
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Registration successful. Please log in." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var token = _tokenService.GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role!.Name,
            }
        });
    }
}

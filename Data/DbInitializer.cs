using ClinicApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicApi.Data;

/// <summary>
/// Seeds an initial Admin account and a few sample doctors/diseases so the
/// app has something to show right after the first migration is applied.
/// Call DbInitializer.SeedAsync(app) once at startup (see Program.cs).
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Roles are seeded via HasData in ApplicationDbContext's OnModelCreating,
        // applied through migrations. Nothing to do here for Roles.

        // ---- Seed default Admin account ----
        if (!await db.Users.AnyAsync(u => u.Email == "admin@medicareclinic.com"))
        {
            var adminRole = await db.Roles.FirstAsync(r => r.Name == "Admin");

            db.Users.Add(new User
            {
                FullName = "Clinic Administrator",
                Email = "admin@medicareclinic.com",
                Phone = "+92 300 0000000",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), // CHANGE after first login
                RoleId = adminRole.Id,
            });

            await db.SaveChangesAsync();
        }

        // ---- Seed a couple of sample doctors, if none exist ----
        if (!await db.Doctors.AnyAsync())
        {
            db.Doctors.AddRange(
                new Doctor
                {
                    Name = "Dr. Ayesha Khan",
                    Qualification = "MBBS, FCPS (Medicine)",
                    Specialization = "General Physician",
                    ExperienceYears = 10,
                    AvailableDays = "Mon, Wed, Fri",
                    AvailableTimeSlots = "9:00 AM - 1:00 PM"
                },
                new Doctor
                {
                    Name = "Dr. Bilal Ahmed",
                    Qualification = "MBBS, FCPS (Cardiology)",
                    Specialization = "Cardiologist",
                    ExperienceYears = 14,
                    AvailableDays = "Tue, Thu, Sat",
                    AvailableTimeSlots = "2:00 PM - 6:00 PM"
                }
            );

            await db.SaveChangesAsync();
        }

        // ---- Seed a couple of sample diseases + treatments, if none exist ----
        if (!await db.Diseases.AnyAsync())
        {
            var flu = new Disease
            {
                Name = "Seasonal Flu",
                Description = "A common viral infection affecting the respiratory system.",
                Symptoms = "Fever, cough, sore throat, body aches, fatigue.",
                Causes = "Influenza virus, spread through airborne droplets."
            };

            db.Diseases.Add(flu);
            await db.SaveChangesAsync();

            db.Treatments.Add(new Treatment
            {
                DiseaseId = flu.Id,
                Procedure = "Rest, fluids, antiviral medication if prescribed early.",
                EstimatedRecoveryTime = "5-7 days"
            });

            await db.SaveChangesAsync();
        }
    }
}

using ClinicApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();
    public DbSet<Disease> Diseases => Set<Disease>();
    public DbSet<Treatment> Treatments => Set<Treatment>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentToken> AppointmentTokens => Set<AppointmentToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Roles ----
        modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "Patient" }
        );

        // ---- Users ----
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Patients (1:1 with User) ----
        modelBuilder.Entity<Patient>()
            .HasOne(p => p.User)
            .WithOne(u => u.Patient)
            .HasForeignKey<Patient>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- DoctorAvailability ----
        modelBuilder.Entity<DoctorAvailability>()
            .HasOne(a => a.Doctor)
            .WithMany(d => d.Availabilities)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- Treatments ----
        modelBuilder.Entity<Treatment>()
            .HasOne(t => t.Disease)
            .WithMany(d => d.Treatments)
            .HasForeignKey(t => t.DiseaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- Appointments ----
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Doctor)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Prevent duplicate bookings: same doctor + date + time slot can only
        // exist once among active (non-cancelled) appointments. SQL Server
        // filtered unique index — cancelled appointments free up the slot.
        modelBuilder.Entity<Appointment>()
            .HasIndex(a => new { a.DoctorId, a.Date, a.TimeSlot })
            .IsUnique()
            .HasFilter("[Status] <> 3"); // 3 = Cancelled

        modelBuilder.Entity<Appointment>()
            .Property(a => a.Status)
            .HasConversion<int>();

        // ---- AppointmentToken (1:1 with Appointment) ----
        modelBuilder.Entity<AppointmentToken>()
            .HasOne(t => t.Appointment)
            .WithOne(a => a.Token)
            .HasForeignKey<AppointmentToken>(t => t.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppointmentToken>()
            .HasIndex(t => t.TokenNumber)
            .IsUnique();
    }
}

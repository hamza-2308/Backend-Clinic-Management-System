using ClinicApi.Data;
using ClinicApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicApi.Services;

public class DuplicateSlotException : Exception
{
    public DuplicateSlotException() : base("This time slot is already booked. Please choose another.") { }
}

public interface IAppointmentService
{
    Task<Appointment> BookAppointmentAsync(int patientId, int doctorId, DateOnly date, TimeSpan timeSlot, string? reason);
}

/// <summary>
/// Encapsulates the automatic queue + token generation workflow described in the task:
/// - Appointment is saved automatically.
/// - Token number is generated automatically (per doctor, per day, e.g. "A-001").
/// - Queue number is assigned automatically (the patient's position in that day's queue).
/// - Time slot is reserved (enforced by a unique DB constraint on Doctor+Date+TimeSlot).
/// - Duplicate bookings for the same slot are rejected.
/// </summary>
public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _db;

    public AppointmentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Appointment> BookAppointmentAsync(int patientId, int doctorId, DateOnly date, TimeSpan timeSlot, string? reason)
    {
        // Use a transaction so the queue-number calculation and the insert are atomic —
        // this prevents two concurrent bookings from getting the same queue number.
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            // Reject duplicate bookings for the same doctor/date/slot up front for a friendly error;
            // the unique filtered index in ApplicationDbContext is the real guarantee under concurrency.
            var slotTaken = await _db.Appointments.AnyAsync(a =>
                a.DoctorId == doctorId &&
                a.Date == date &&
                a.TimeSlot == timeSlot &&
                a.Status != AppointmentStatus.Cancelled);

            if (slotTaken)
                throw new DuplicateSlotException();

            // Queue number = count of active appointments for this doctor on this date, + 1
            var queueCount = await _db.Appointments.CountAsync(a =>
                a.DoctorId == doctorId &&
                a.Date == date &&
                a.Status != AppointmentStatus.Cancelled);

            var queueNumber = queueCount + 1;

            var appointment = new Appointment
            {
                PatientId = patientId,
                DoctorId = doctorId,
                Date = date,
                TimeSlot = timeSlot,
                Reason = reason,
                Status = AppointmentStatus.Pending,
                QueueNumber = queueNumber,
            };

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync();

            // Token format: "<DoctorInitial>-<3-digit queue number>", e.g. "D3-014"
            var tokenNumber = $"D{doctorId}-{queueNumber:D3}-{date:yyMMdd}";

            var token = new AppointmentToken
            {
                AppointmentId = appointment.Id,
                TokenNumber = tokenNumber,
            };

            _db.AppointmentTokens.Add(token);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            appointment.Token = token;
            return appointment;
        }
        catch (DbUpdateException) 
        {
            // The unique index caught a race condition another request won.
            await transaction.RollbackAsync();
            throw new DuplicateSlotException();
        }
        catch (DuplicateSlotException)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

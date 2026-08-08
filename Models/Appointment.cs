namespace ClinicApi.Models;

public class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public DateOnly Date { get; set; }
    public TimeSpan TimeSlot { get; set; }

    public string? Reason { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    // Queue number: patient's position in that doctor's queue for that day (1, 2, 3, ...)
    public int QueueNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // One-to-one: each appointment gets exactly one generated token
    public AppointmentToken? Token { get; set; }
}

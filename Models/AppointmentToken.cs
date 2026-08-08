namespace ClinicApi.Models;

public class AppointmentToken
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    // Human-friendly token, e.g. "A-014" — unique per doctor per day
    public string TokenNumber { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

namespace ClinicApi.Models;

public class Doctor
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public string? PhotoUrl { get; set; }

    // Denormalized, human-readable summaries (e.g. "Mon, Wed, Fri" / "9AM-1PM, 4PM-8PM")
    // used for quick display; the DoctorAvailability table holds structured slot data.
    public string? AvailableDays { get; set; }
    public string? AvailableTimeSlots { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<DoctorAvailability> Availabilities { get; set; } = new List<DoctorAvailability>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}

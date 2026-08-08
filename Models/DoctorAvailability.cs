namespace ClinicApi.Models;

public class DoctorAvailability
{
    public int Id { get; set; }

    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    // DayOfWeek as int (0=Sunday..6=Saturday) so it can recur weekly
    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    // Length of each bookable slot, e.g. 30 minutes
    public int SlotDurationMinutes { get; set; } = 30;
}

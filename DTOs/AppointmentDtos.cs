namespace ClinicApi.DTOs;

public class BookAppointmentDto
{
    public int DoctorId { get; set; }
    public DateOnly Date { get; set; }

    // e.g. "09:00 AM" — parsed server-side into a TimeSpan
    public string TimeSlot { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class AppointmentDto
{
    public int Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string TimeSlot { get; set; } = string.Empty;
    public int QueueNumber { get; set; }
    public string TokenNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class RescheduleDto
{
    public DateOnly Date { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
}

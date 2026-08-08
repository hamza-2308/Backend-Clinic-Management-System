namespace ClinicApi.DTOs;

public class DoctorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public string? PhotoUrl { get; set; }
    public string? AvailableDays { get; set; }
    public string? AvailableTimeSlots { get; set; }
}



public class DoctorUpsertDto
{
    public string Name { get; set; } = string.Empty;

    public string Qualification { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public int ExperienceYears { get; set; }

    public string? AvailableDays { get; set; }

    public string? AvailableTimeSlots { get; set; }

    public IFormFile? Photo { get; set; }
}

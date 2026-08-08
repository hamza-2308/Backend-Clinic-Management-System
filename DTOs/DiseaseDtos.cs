namespace ClinicApi.DTOs;

public class DiseaseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Symptoms { get; set; }
    public string? Causes { get; set; }

    // Flattened from the primary Treatment record for simple display
    public string? TreatmentProcedure { get; set; }
    public string? RecoveryTime { get; set; }
}

public class DiseaseUpsertDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Symptoms { get; set; }
    public string? Causes { get; set; }
    public string? TreatmentProcedure { get; set; }
    public string? RecoveryTime { get; set; }
}

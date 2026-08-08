namespace ClinicApi.Models;

public class Treatment
{
    public int Id { get; set; }

    public int DiseaseId { get; set; }
    public Disease? Disease { get; set; }

    public string Procedure { get; set; } = string.Empty;
    public string? EstimatedRecoveryTime { get; set; }
    public string? Notes { get; set; }
}

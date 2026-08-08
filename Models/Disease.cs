namespace ClinicApi.Models;

public class Disease
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Symptoms { get; set; }
    public string? Causes { get; set; }

    public ICollection<Treatment> Treatments { get; set; } = new List<Treatment>();
}

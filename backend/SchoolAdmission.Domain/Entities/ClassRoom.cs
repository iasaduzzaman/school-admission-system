namespace SchoolAdmission.Domain.Entities;

public class ClassRoom
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AdmissionHold> Holds { get; set; } = new List<AdmissionHold>();
}

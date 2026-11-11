using SchoolAdmission.Domain.Enums;

namespace SchoolAdmission.Domain.Entities;

public class AdmissionHold
{
    public string Id { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public ClassRoom? ClassRoom { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string Status { get; set; } = HoldStatus.Held;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public bool IsActiveHold(DateTime utcNow)
    {
        return Status == HoldStatus.Held && ExpiresAt > utcNow;
    }

    public string GetEffectiveStatus(DateTime utcNow)
    {
        if (Status == HoldStatus.Held && ExpiresAt <= utcNow)
        {
            return HoldStatus.Expired;
        }
        return Status;
    }
}

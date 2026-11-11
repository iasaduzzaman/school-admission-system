namespace SchoolAdmission.Domain.Common;

public interface ISystemClock
{
    DateTime UtcNow { get; }
}

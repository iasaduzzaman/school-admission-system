using SchoolAdmission.Domain.Common;

namespace SchoolAdmission.Infrastructure.Common;

public class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

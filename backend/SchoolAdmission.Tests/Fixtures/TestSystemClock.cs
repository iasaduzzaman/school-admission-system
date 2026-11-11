using SchoolAdmission.Domain.Common;

namespace SchoolAdmission.Tests.Fixtures;

public class TestSystemClock : ISystemClock
{
    private DateTime _utcNow = DateTime.UtcNow;

    public DateTime UtcNow
    {
        get => _utcNow;
        set => _utcNow = value;
    }

    public void Advance(TimeSpan duration)
    {
        _utcNow = _utcNow.Add(duration);
    }

    public void Reset()
    {
        _utcNow = DateTime.UtcNow;
    }
}

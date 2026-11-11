namespace SchoolAdmission.Domain.Enums;

public static class HoldStatus
{
    public const string Held = "HELD";
    public const string Confirmed = "CONFIRMED";
    public const string Expired = "EXPIRED";
    public const string Cancelled = "CANCELLED";

    public static readonly string[] All = [Held, Confirmed, Expired, Cancelled];
}

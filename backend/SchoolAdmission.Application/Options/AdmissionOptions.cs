namespace SchoolAdmission.Application.Options;

public class AdmissionOptions
{
    public const string HoldDurationSecondsEnv = "HOLD_DURATION_SECONDS";
    public int HoldDurationSeconds { get; set; } = 600;
}

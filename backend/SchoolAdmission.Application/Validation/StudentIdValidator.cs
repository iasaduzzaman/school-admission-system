using System.Text.RegularExpressions;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Application.Validation;

public static class StudentIdValidator
{
    private static readonly Regex DigitRegex = new(@"^\d{17}$", RegexOptions.Compiled);

    public static string ValidateAndNormalize(string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            throw new ValidationException("X-Student-Id header is required.");
        }

        var trimmed = studentId.Trim();
        if (!DigitRegex.IsMatch(trimmed))
        {
            throw new ValidationException("Student ID must be exactly a 17-digit birth registration number.");
        }

        return trimmed;
    }
}

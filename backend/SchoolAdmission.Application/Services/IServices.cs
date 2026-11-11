using SchoolAdmission.Application.DTOs;

namespace SchoolAdmission.Application.Services;

public interface IClassService
{
    Task<ClassResponse> CreateClassAsync(CreateClassRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ClassDetailsResponse>> GetAllClassesAsync(CancellationToken ct = default);
    Task<ClassDetailsResponse> GetClassByIdAsync(string classId, CancellationToken ct = default);
}

public interface IHoldService
{
    Task<HoldResponse> CreateHoldAsync(string classId, string studentId, CancellationToken ct = default);
    Task<HoldDetailsResponse> GetHoldByIdAsync(string holdId, CancellationToken ct = default);
    Task<ConfirmHoldResponse> ConfirmHoldAsync(string holdId, string studentId, CancellationToken ct = default);
    Task CancelHoldAsync(string holdId, string studentId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentHoldResponse>> GetStudentHoldsAsync(string studentId, CancellationToken ct = default);
}

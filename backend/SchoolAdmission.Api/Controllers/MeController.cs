using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Services;
using SchoolAdmission.Application.Validation;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("me")]
public class MeController : ControllerBase
{
    private readonly IHoldService _holdService;

    public MeController(IHoldService holdService)
    {
        _holdService = holdService;
    }

    [HttpGet("holds")]
    public async Task<ActionResult<IReadOnlyList<StudentHoldResponse>>> GetStudentHolds(
        [FromHeader(Name = "X-Student-Id")] string? studentIdHeader,
        CancellationToken ct)
    {
        var studentId = StudentIdValidator.ValidateAndNormalize(studentIdHeader);
        var result = await _holdService.GetStudentHoldsAsync(studentId, ct);
        return Ok(result);
    }
}

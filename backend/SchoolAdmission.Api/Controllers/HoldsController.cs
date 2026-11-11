using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Services;
using SchoolAdmission.Application.Validation;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("holds")]
public class HoldsController : ControllerBase
{
    private readonly IHoldService _holdService;

    public HoldsController(IHoldService holdService)
    {
        _holdService = holdService;
    }

    [HttpGet("{holdId}")]
    public async Task<ActionResult<HoldDetailsResponse>> GetHoldById(
        string holdId,
        CancellationToken ct)
    {
        var result = await _holdService.GetHoldByIdAsync(holdId, ct);
        return Ok(result);
    }

    [HttpPost("{holdId}/confirm")]
    public async Task<ActionResult<ConfirmHoldResponse>> ConfirmHold(
        string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentIdHeader,
        CancellationToken ct)
    {
        var studentId = StudentIdValidator.ValidateAndNormalize(studentIdHeader);
        var result = await _holdService.ConfirmHoldAsync(holdId, studentId, ct);
        return Ok(result);
    }

    [HttpDelete("{holdId}")]
    public async Task<IActionResult> CancelHold(
        string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentIdHeader,
        CancellationToken ct)
    {
        var studentId = StudentIdValidator.ValidateAndNormalize(studentIdHeader);
        await _holdService.CancelHoldAsync(holdId, studentId, ct);
        return NoContent();
    }
}

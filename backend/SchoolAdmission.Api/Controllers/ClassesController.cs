using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Services;
using SchoolAdmission.Application.Validation;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly IClassService _classService;
    private readonly IHoldService _holdService;

    public ClassesController(IClassService classService, IHoldService holdService)
    {
        _classService = classService;
        _holdService = holdService;
    }

    [HttpPost]
    public async Task<ActionResult<ClassResponse>> CreateClass(
        [FromBody] CreateClassRequest request,
        CancellationToken ct)
    {
        var result = await _classService.CreateClassAsync(request, ct);
        return Created($"/classes/{result.Id}", result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClassDetailsResponse>>> GetAllClasses(CancellationToken ct)
    {
        var result = await _classService.GetAllClassesAsync(ct);
        return Ok(result);
    }

    [HttpGet("{classId}")]
    public async Task<ActionResult<ClassDetailsResponse>> GetClassById(
        string classId,
        CancellationToken ct)
    {
        var result = await _classService.GetClassByIdAsync(classId, ct);
        return Ok(result);
    }

    [HttpPost("{classId}/holds")]
    public async Task<ActionResult<HoldResponse>> CreateHold(
        string classId,
        [FromHeader(Name = "X-Student-Id")] string? studentIdHeader,
        CancellationToken ct)
    {
        var studentId = StudentIdValidator.ValidateAndNormalize(studentIdHeader);
        var result = await _holdService.CreateHoldAsync(classId, studentId, ct);
        return Created($"/holds/{result.HoldId}", result);
    }
}

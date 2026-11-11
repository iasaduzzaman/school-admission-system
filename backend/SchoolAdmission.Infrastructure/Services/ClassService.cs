using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Services;
using SchoolAdmission.Domain.Common;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;
using SchoolAdmission.Infrastructure.Data;

namespace SchoolAdmission.Infrastructure.Services;

public class ClassService : IClassService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ISystemClock _systemClock;

    public ClassService(ApplicationDbContext dbContext, ISystemClock systemClock)
    {
        _dbContext = dbContext;
        _systemClock = systemClock;
    }

    public async Task<ClassResponse> CreateClassAsync(CreateClassRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Class name cannot be empty.");
        }

        if (request.TotalSeats <= 0)
        {
            throw new ValidationException("Total seats must be greater than zero.");
        }

        var now = _systemClock.UtcNow;
        var classRoom = new ClassRoom
        {
            Id = "c_" + Guid.NewGuid().ToString("N")[..8],
            Name = request.Name.Trim(),
            TotalSeats = request.TotalSeats,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Classes.Add(classRoom);
        await _dbContext.SaveChangesAsync(ct);

        return new ClassResponse
        {
            Id = classRoom.Id,
            Name = classRoom.Name,
            TotalSeats = classRoom.TotalSeats
        };
    }

    public async Task<IReadOnlyList<ClassDetailsResponse>> GetAllClassesAsync(CancellationToken ct = default)
    {
        var now = _systemClock.UtcNow;

        var classes = await _dbContext.Classes
            .AsNoTracking()
            .Include(c => c.Holds)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        return classes.Select(c =>
        {
            var confirmed = c.Holds.Count(h => h.Status == HoldStatus.Confirmed);
            var held = c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now);
            var available = Math.Max(0, c.TotalSeats - (confirmed + held));

            return new ClassDetailsResponse
            {
                Id = c.Id,
                Name = c.Name,
                TotalSeats = c.TotalSeats,
                HeldSeats = held,
                ConfirmedSeats = confirmed,
                AvailableSeats = available
            };
        }).ToList();
    }

    public async Task<ClassDetailsResponse> GetClassByIdAsync(string classId, CancellationToken ct = default)
    {
        var now = _systemClock.UtcNow;

        var classRoom = await _dbContext.Classes
            .AsNoTracking()
            .Include(c => c.Holds)
            .FirstOrDefaultAsync(c => c.Id == classId, ct);

        if (classRoom == null)
        {
            throw new NotFoundException($"Class with ID '{classId}' was not found.");
        }

        var confirmed = classRoom.Holds.Count(h => h.Status == HoldStatus.Confirmed);
        var held = classRoom.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now);
        var available = Math.Max(0, classRoom.TotalSeats - (confirmed + held));

        return new ClassDetailsResponse
        {
            Id = classRoom.Id,
            Name = classRoom.Name,
            TotalSeats = classRoom.TotalSeats,
            HeldSeats = held,
            ConfirmedSeats = confirmed,
            AvailableSeats = available
        };
    }
}

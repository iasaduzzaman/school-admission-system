using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Options;
using SchoolAdmission.Application.Services;
using SchoolAdmission.Domain.Common;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;
using SchoolAdmission.Infrastructure.Data;

namespace SchoolAdmission.Infrastructure.Services;

public class HoldService : IHoldService
{
    private static readonly SemaphoreSlim _nonNpgsqlLock = new(1, 1);
    private readonly ApplicationDbContext _dbContext;
    private readonly ISystemClock _systemClock;
    private readonly AdmissionOptions _options;

    public HoldService(
        ApplicationDbContext dbContext,
        ISystemClock systemClock,
        IOptions<AdmissionOptions> options)
    {
        _dbContext = dbContext;
        _systemClock = systemClock;
        _options = options.Value;
    }

    public async Task<HoldResponse> CreateHoldAsync(string classId, string studentId, CancellationToken ct = default)
    {
        var isNpgsql = _dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        if (!isNpgsql)
        {
            await _nonNpgsqlLock.WaitAsync(ct);
        }

        try
        {
            var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

                // 1. Acquire pessimistic row lock on the class in PostgreSQL
                ClassRoom? classRoom;
                if (isNpgsql)
                {
                    await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"SELECT 1 FROM classes WHERE \"Id\" = {classId} FOR UPDATE", ct);
                    classRoom = await _dbContext.Classes.FirstOrDefaultAsync(c => c.Id == classId, ct);
                }
                else
                {
                    classRoom = await _dbContext.Classes.FirstOrDefaultAsync(c => c.Id == classId, ct);
                }

                if (classRoom == null)
                {
                    throw new NotFoundException($"Class with ID '{classId}' was not found.");
                }

                var now = _systemClock.UtcNow;

                // 2. Check if student already has a confirmed admission or active hold for this class
                var studentHasActiveHoldOrConfirmed = await _dbContext.AdmissionHolds
                    .AnyAsync(h => h.ClassId == classId &&
                                   h.StudentId == studentId &&
                                   (h.Status == HoldStatus.Confirmed || (h.Status == HoldStatus.Held && h.ExpiresAt > now)),
                              ct);

                if (studentHasActiveHoldOrConfirmed)
                {
                    throw new ConflictException("Student already has an active reservation or confirmed admission for this class.");
                }

                // 3. Concurrency check: calculate occupied seats
                var confirmedSeats = await _dbContext.AdmissionHolds
                    .CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.Confirmed, ct);

                var heldSeats = await _dbContext.AdmissionHolds
                    .CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt > now, ct);

                var availableSeats = classRoom.TotalSeats - (confirmedSeats + heldSeats);

                if (availableSeats <= 0)
                {
                    throw new ConflictException("No seats available for this class.");
                }

                // 4. Create new hold
                var holdId = "h_" + Guid.NewGuid().ToString("N")[..8];
                var expiresAt = now.AddSeconds(_options.HoldDurationSeconds);

                var hold = new AdmissionHold
                {
                    Id = holdId,
                    ClassId = classId,
                    StudentId = studentId,
                    Status = HoldStatus.Held,
                    CreatedAt = now,
                    ExpiresAt = expiresAt
                };

                _dbContext.AdmissionHolds.Add(hold);
                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new HoldResponse
                {
                    HoldId = hold.Id,
                    ClassId = hold.ClassId,
                    StudentId = hold.StudentId,
                    Status = hold.Status,
                    ExpiresAt = hold.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
            });
        }
        finally
        {
            if (!isNpgsql)
            {
                _nonNpgsqlLock.Release();
            }
        }
    }

    public async Task<HoldDetailsResponse> GetHoldByIdAsync(string holdId, CancellationToken ct = default)
    {
        var hold = await _dbContext.AdmissionHolds
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == holdId, ct);

        if (hold == null)
        {
            throw new NotFoundException($"Reservation with ID '{holdId}' was not found.");
        }

        var effectiveStatus = hold.GetEffectiveStatus(_systemClock.UtcNow);

        return new HoldDetailsResponse
        {
            HoldId = hold.Id,
            ClassId = hold.ClassId,
            Status = effectiveStatus,
            ExpiresAt = hold.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    public async Task<ConfirmHoldResponse> ConfirmHoldAsync(string holdId, string studentId, CancellationToken ct = default)
    {
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            AdmissionHold? hold;
            if (_dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
            {
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM admission_holds WHERE \"Id\" = {holdId} FOR UPDATE", ct);
                hold = await _dbContext.AdmissionHolds.FirstOrDefaultAsync(h => h.Id == holdId, ct);
            }
            else
            {
                hold = await _dbContext.AdmissionHolds.FirstOrDefaultAsync(h => h.Id == holdId, ct);
            }

            if (hold == null)
            {
                throw new NotFoundException($"Reservation with ID '{holdId}' was not found.");
            }

            // Ownership validation
            if (hold.StudentId != studentId)
            {
                throw new ForbiddenException("You can only confirm your own reservation.");
            }

            // Idempotent: If already confirmed, return success without altering confirmed seats
            if (hold.Status == HoldStatus.Confirmed)
            {
                return new ConfirmHoldResponse
                {
                    HoldId = hold.Id,
                    Status = HoldStatus.Confirmed
                };
            }

            var now = _systemClock.UtcNow;

            if (hold.Status == HoldStatus.Cancelled)
            {
                throw new ConflictException("Cannot confirm a cancelled reservation.");
            }

            if (hold.Status != HoldStatus.Held || hold.ExpiresAt <= now)
            {
                if (hold.Status == HoldStatus.Held)
                {
                    hold.Status = HoldStatus.Expired;
                    await _dbContext.SaveChangesAsync(ct);
                }
                await transaction.CommitAsync(ct);
                throw new ConflictException("Cannot confirm an expired reservation.");
            }

            hold.Status = HoldStatus.Confirmed;
            hold.ConfirmedAt = now;

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return new ConfirmHoldResponse
            {
                HoldId = hold.Id,
                Status = HoldStatus.Confirmed
            };
        });
    }

    public async Task CancelHoldAsync(string holdId, string studentId, CancellationToken ct = default)
    {
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            AdmissionHold? hold;
            if (_dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
            {
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM admission_holds WHERE \"Id\" = {holdId} FOR UPDATE", ct);
                hold = await _dbContext.AdmissionHolds.FirstOrDefaultAsync(h => h.Id == holdId, ct);
            }
            else
            {
                hold = await _dbContext.AdmissionHolds.FirstOrDefaultAsync(h => h.Id == holdId, ct);
            }

            if (hold == null)
            {
                throw new NotFoundException($"Reservation with ID '{holdId}' was not found.");
            }

            // Ownership validation
            if (hold.StudentId != studentId)
            {
                throw new ForbiddenException("You can only cancel your own reservation.");
            }

            if (hold.Status == HoldStatus.Confirmed)
            {
                throw new ConflictException("Cannot cancel an already confirmed admission.");
            }

            if (hold.Status == HoldStatus.Cancelled)
            {
                // Already cancelled - idempotent success
                return;
            }

            hold.Status = HoldStatus.Cancelled;
            hold.CancelledAt = _systemClock.UtcNow;

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        });
    }

    public async Task<IReadOnlyList<StudentHoldResponse>> GetStudentHoldsAsync(string studentId, CancellationToken ct = default)
    {
        var now = _systemClock.UtcNow;

        var holds = await _dbContext.AdmissionHolds
            .AsNoTracking()
            .Include(h => h.ClassRoom)
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(ct);

        return holds.Select(h => new StudentHoldResponse
        {
            HoldId = h.Id,
            ClassId = h.ClassId,
            ClassName = h.ClassRoom?.Name ?? string.Empty,
            Status = h.GetEffectiveStatus(now),
            ExpiresAt = h.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        }).ToList();
    }
}

using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;

namespace SchoolAdmission.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<ClassRoom> Classes => Set<ClassRoom>();
    public DbSet<AdmissionHold> AdmissionHolds => Set<AdmissionHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClassRoom>(entity =>
        {
            entity.ToTable("classes");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TotalSeats).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasMany(e => e.Holds)
                .WithOne(h => h.ClassRoom)
                .HasForeignKey(h => h.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AdmissionHold>(entity =>
        {
            entity.ToTable("admission_holds");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.ClassId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.StudentId).HasMaxLength(17).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.ConfirmedAt);
            entity.Property(e => e.CancelledAt);

            entity.HasIndex(e => new { e.ClassId, e.Status, e.ExpiresAt })
                .HasDatabaseName("IX_admission_holds_class_status_expires");

            entity.HasIndex(e => e.StudentId)
                .HasDatabaseName("IX_admission_holds_student");

            entity.HasIndex(e => new { e.ClassId, e.StudentId })
                .HasDatabaseName("UQ_admission_holds_class_student_confirmed")
                .HasFilter($"\"Status\" = '{HoldStatus.Confirmed}'")
                .IsUnique();
        });
    }
}

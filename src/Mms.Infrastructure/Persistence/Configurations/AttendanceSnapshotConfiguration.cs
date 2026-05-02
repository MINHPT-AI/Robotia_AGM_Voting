using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class AttendanceSnapshotConfiguration : IEntityTypeConfiguration<AttendanceSnapshot>
{
    public void Configure(EntityTypeBuilder<AttendanceSnapshot> builder)
    {
        builder.ToTable("attendance_snapshots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SnapshotType).IsRequired().HasMaxLength(20);
        builder.Property(s => s.PercentageQuorum).HasPrecision(5, 2);

        builder.HasIndex(s => new { s.MeetingId, s.SnapshotType }).IsUnique();

        builder.HasOne(s => s.Meeting)
            .WithMany()
            .HasForeignKey(s => s.MeetingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.PhysicalAttendeeIdNumber).IsRequired().HasMaxLength(50);
        builder.Property(a => a.PhysicalAttendeeName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.AttendanceType).HasConversion<string>();
        builder.Property(a => a.AttendCode).IsRequired();
        builder.Property(a => a.PhoneNumber).HasMaxLength(20);
        builder.Property(a => a.PhoneSource).HasConversion<string>();

        // RB-04: mỗi cổ đông chỉ có 1 Phiên tham dự ACTIVE tại 1 cuộc họp
        builder.HasIndex(a => new { a.MeetingId, a.ShareholderId })
            .IsUnique()
            .HasFilter("\"IsActive\" = true");

        builder.HasIndex(a => a.AttendCode).IsUnique();
        builder.HasIndex(a => a.PhysicalAttendeeIdNumber);

        builder.HasOne(a => a.Meeting)
            .WithMany()
            .HasForeignKey(a => a.MeetingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Shareholder)
            .WithMany()
            .HasForeignKey(a => a.ShareholderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optimistic concurrency
        builder.Property(a => a.Xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
    }
}

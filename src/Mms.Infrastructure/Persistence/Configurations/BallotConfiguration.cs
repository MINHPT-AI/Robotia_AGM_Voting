using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class BallotConfiguration : IEntityTypeConfiguration<Ballot>
{
    public void Configure(EntityTypeBuilder<Ballot> builder)
    {
        builder.ToTable("ballots");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.AttendCode).IsRequired();
        builder.Property(b => b.Status).HasConversion<string>();
        builder.Property(b => b.BallotType).HasConversion<string>();
        builder.Property(b => b.ProxyRepresentationNote).HasMaxLength(1000);

        builder.HasIndex(b => b.AttendCode).IsUnique();
        builder.HasIndex(b => new { b.MeetingId, b.Status });
        builder.HasIndex(b => new { b.AttendanceRecordId, b.BallotType });
        // Filtered index for reprint queue — added via raw SQL in migration

        builder.HasOne(b => b.Meeting)
            .WithMany()
            .HasForeignKey(b => b.MeetingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Shareholder)
            .WithMany()
            .HasForeignKey(b => b.ShareholderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.ParentBallot)
            .WithMany()
            .HasForeignKey(b => b.ParentBallotId)
            .IsRequired(false);

        builder.HasOne(b => b.AttendanceRecord)
            .WithMany(a => a.Ballots)
            .HasForeignKey(b => b.AttendanceRecordId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Optimistic concurrency via Postgres xmin system column
        builder.Property(b => b.Xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
    }
}

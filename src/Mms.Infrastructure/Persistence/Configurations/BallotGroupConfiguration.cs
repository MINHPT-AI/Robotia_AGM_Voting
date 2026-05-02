using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class BallotGroupConfiguration : IEntityTypeConfiguration<BallotGroup>
{
    public void Configure(EntityTypeBuilder<BallotGroup> builder)
    {
        builder.ToTable("ballot_groups");
        builder.HasKey(g => g.Id);

        // RB-11: mỗi CĐ nguồn chỉ xuất hiện trong 1 nhóm phiếu tách
        builder.HasIndex(g => new { g.AttendanceRecordId, g.SourceShareholderId }).IsUnique();

        builder.HasOne(g => g.AttendanceRecord)
            .WithMany(a => a.BallotGroups)
            .HasForeignKey(g => g.AttendanceRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.Ballot)
            .WithMany()
            .HasForeignKey(g => g.BallotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.SourceShareholder)
            .WithMany()
            .HasForeignKey(g => g.SourceShareholderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

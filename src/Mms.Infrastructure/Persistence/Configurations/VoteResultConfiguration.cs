using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class VoteResultConfiguration : IEntityTypeConfiguration<VoteResult>
{
    public void Configure(EntityTypeBuilder<VoteResult> builder)
    {
        builder.ToTable("vote_results");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.VoteChoice).HasConversion<string>();

        // Mỗi phiếu chỉ có 1 kết quả cho mỗi nội dung NQ
        builder.HasIndex(v => new { v.BallotId, v.MeetingResolutionId }).IsUnique();

        builder.HasOne(v => v.Ballot)
            .WithMany(b => b.VoteResults)
            .HasForeignKey(v => v.BallotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.MeetingResolution)
            .WithMany()
            .HasForeignKey(v => v.MeetingResolutionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

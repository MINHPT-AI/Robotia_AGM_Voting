using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class ElectionVoteConfiguration : IEntityTypeConfiguration<ElectionVote>
{
    public void Configure(EntityTypeBuilder<ElectionVote> builder)
    {
        builder.ToTable("election_votes");
        builder.HasKey(v => v.Id);

        // Mỗi phiếu chỉ bầu 1 lần cho mỗi ứng viên
        builder.HasIndex(v => new { v.BallotId, v.MeetingCandidateId }).IsUnique();

        builder.HasOne(v => v.Ballot)
            .WithMany(b => b.ElectionVotes)
            .HasForeignKey(v => v.BallotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.MeetingCandidate)
            .WithMany()
            .HasForeignKey(v => v.MeetingCandidateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

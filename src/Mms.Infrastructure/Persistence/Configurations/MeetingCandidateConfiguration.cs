using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class MeetingCandidateConfiguration : IEntityTypeConfiguration<MeetingCandidate>
{
    public void Configure(EntityTypeBuilder<MeetingCandidate> builder)
    {
        builder.ToTable("meeting_candidates");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FullName).IsRequired();
        builder.Property(c => c.Position).IsRequired();
        builder.Property(c => c.CurrentPosition).HasMaxLength(255);
        builder.Property(c => c.CandidateBoard).HasConversion<string>();

        builder.HasOne(c => c.Meeting)
            .WithMany(m => m.Candidates)
            .HasForeignKey(c => c.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

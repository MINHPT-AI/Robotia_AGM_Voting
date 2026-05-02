using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class MeetingResolutionConfiguration : IEntityTypeConfiguration<MeetingResolution>
{
    public void Configure(EntityTypeBuilder<MeetingResolution> builder)
    {
        builder.ToTable("meeting_resolutions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Title).IsRequired();
        builder.Property(r => r.ResolutionType).HasConversion<string>();
        builder.Property(r => r.ApprovalThreshold).HasPrecision(5, 4);

        builder.HasOne(r => r.Meeting)
            .WithMany(m => m.Resolutions)
            .HasForeignKey(r => r.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

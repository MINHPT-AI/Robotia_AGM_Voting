using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class InvitationLetterConfiguration : IEntityTypeConfiguration<InvitationLetter>
{
    public void Configure(EntityTypeBuilder<InvitationLetter> builder)
    {
        builder.ToTable("invitation_letters");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ShareholderIdNumber).IsRequired().HasMaxLength(50);
        builder.Property(l => l.ShareholderName).IsRequired().HasMaxLength(300);
        builder.Property(l => l.ShareholderAddress).HasMaxLength(500);
        builder.Property(l => l.ShareholderPhone).HasMaxLength(50);
        builder.Property(l => l.TrackingCode).HasMaxLength(100);
        builder.Property(l => l.FailureReason).HasMaxLength(500);

        builder.Property(l => l.Status)
            .HasConversion<int>()
            .HasDefaultValue(InvitationStatus.NotSent);

        builder.Property(l => l.CodeMarkType)
            .HasConversion<int>()
            .HasDefaultValue(CodeMarkType.Barcode);

        builder.HasOne(l => l.Meeting)
            .WithMany(m => m.InvitationLetters)
            .HasForeignKey(l => l.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for meeting-level queries
        builder.HasIndex(l => l.MeetingId);

        // Index for tracking code lookup (CPN matching)
        builder.HasIndex(l => l.TrackingCode)
            .HasFilter("\"TrackingCode\" IS NOT NULL");

        // Index for status filtering
        builder.HasIndex(l => new { l.MeetingId, l.Status });
    }
}

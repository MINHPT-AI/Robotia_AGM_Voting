using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class ShareholderConfiguration : IEntityTypeConfiguration<Shareholder>
{
    public void Configure(EntityTypeBuilder<Shareholder> builder)
    {
        builder.ToTable("shareholders");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.FullName).IsRequired();
        builder.Property(s => s.IdNumber).IsRequired();
        builder.Property(s => s.VsdcRow).HasMaxLength(20);

        builder.HasOne(s => s.Meeting)
            .WithMany(m => m.Shareholders)
            .HasForeignKey(s => s.MeetingId)
            .OnDelete(DeleteBehavior.Restrict);

        // Non-unique index for query performance (removed unique—VSDC allows same ID with different issue dates)
        builder.HasIndex(s => new { s.MeetingId, s.IdNumber });

        // Sort index for display order
        builder.HasIndex(s => new { s.MeetingId, s.DisplayOrder });
    }
}

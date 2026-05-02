using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class TallySnapshotConfiguration : IEntityTypeConfiguration<TallySnapshot>
{
    public void Configure(EntityTypeBuilder<TallySnapshot> builder)
    {
        builder.ToTable("tally_snapshots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SnapshotType).IsRequired().HasMaxLength(20);

        builder.HasIndex(s => new { s.MeetingId, s.SnapshotType }).IsUnique();

        builder.HasOne(s => s.Meeting)
            .WithMany()
            .HasForeignKey(s => s.MeetingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

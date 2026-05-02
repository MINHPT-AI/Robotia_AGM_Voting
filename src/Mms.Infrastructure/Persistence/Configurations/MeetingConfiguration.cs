using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("meetings");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Title).IsRequired();
        builder.Property(m => m.Location).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>();
        builder.Property(m => m.MeetingType).HasConversion<string>();
        builder.Property(m => m.DefaultPrintMode).HasConversion<string>();
        builder.Property(m => m.QuorumThreshold).HasPrecision(5, 4);

        builder.HasOne(m => m.Company)
            .WithMany(c => c.Meetings)
            .HasForeignKey(m => m.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.CompanyId);
        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}

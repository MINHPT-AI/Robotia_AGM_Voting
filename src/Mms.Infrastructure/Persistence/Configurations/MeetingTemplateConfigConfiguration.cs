using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class MeetingTemplateConfigConfiguration : IEntityTypeConfiguration<MeetingTemplateConfig>
{
    public void Configure(EntityTypeBuilder<MeetingTemplateConfig> builder)
    {
        builder.ToTable("meeting_template_configs");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TemplateType).HasConversion<string>();
        builder.Property(c => c.CodeType).HasConversion<string>();

        // Mỗi cuộc họp chỉ có 1 config cho mỗi loại template
        builder.HasIndex(c => new { c.MeetingId, c.TemplateType }).IsUnique();

        builder.HasOne(c => c.Meeting)
            .WithMany(m => m.TemplateConfigs)
            .HasForeignKey(c => c.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Template)
            .WithMany()
            .HasForeignKey(c => c.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseIdentityAlwaysColumn(); // BIGSERIAL
        builder.Property(a => a.Actor).IsRequired();
        builder.Property(a => a.Category).HasConversion<string>();
        builder.Property(a => a.Detail).HasColumnType("jsonb");

        builder.HasIndex(a => new { a.MeetingId, a.Ts });
        // DB trigger preventing UPDATE/DELETE is added in migration SQL
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class ProxyRecipientConfiguration : IEntityTypeConfiguration<ProxyRecipient>
{
    public void Configure(EntityTypeBuilder<ProxyRecipient> builder)
    {
        builder.ToTable("proxy_recipients");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.FullName).IsRequired().HasMaxLength(200);
        builder.Property(r => r.IdNumber).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Organization).HasMaxLength(300);
        builder.Property(r => r.Position).HasMaxLength(200);
        builder.Property(r => r.PhoneNumber).HasMaxLength(20);

        builder.HasIndex(r => r.IdNumber);
    }
}

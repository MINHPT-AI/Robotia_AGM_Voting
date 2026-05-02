using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class ProxyConfiguration : IEntityTypeConfiguration<Proxy>
{
    public void Configure(EntityTypeBuilder<Proxy> builder)
    {
        builder.ToTable("proxies");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.GranteeName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.GranteeIdNumber).HasMaxLength(50);
        builder.Property(p => p.Scope).HasConversion<string>();
        builder.Property(p => p.ProxyType).HasConversion<string>();
        builder.Property(p => p.Status).HasConversion<string>();
        builder.Property(p => p.CancellationReason).HasMaxLength(500);

        builder.HasIndex(p => new { p.MeetingId, p.GrantorId });
        builder.HasIndex(p => p.Status);

        builder.HasOne(p => p.Meeting)
            .WithMany()
            .HasForeignKey(p => p.MeetingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Grantor)
            .WithMany()
            .HasForeignKey(p => p.GrantorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.GranteeShareholder)
            .WithMany()
            .HasForeignKey(p => p.GranteeShareholderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.GranteeRecipient)
            .WithMany()
            .HasForeignKey(p => p.GranteeRecipientId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.SupersededBy)
            .WithMany()
            .HasForeignKey(p => p.SupersededById)
            .IsRequired(false);
    }
}

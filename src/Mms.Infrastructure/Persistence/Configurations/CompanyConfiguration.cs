using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mms.Domain.Entities;

namespace Mms.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired();
        builder.Property(c => c.TaxCode).IsRequired();
        builder.Property(c => c.LegalRepName).IsRequired();
        builder.Property(c => c.LegalRepTitle).IsRequired();
        builder.Property(c => c.EnglishName).HasMaxLength(255);
        builder.Property(c => c.StockExchange).HasMaxLength(10);
        builder.HasIndex(c => c.TaxCode).IsUnique();
    }
}

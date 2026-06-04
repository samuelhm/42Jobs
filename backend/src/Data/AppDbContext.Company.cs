using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Data;

public partial class AppDbContext
{
    private static void ConfigureCompany(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).ValueGeneratedOnAdd();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(500);
            entity.HasIndex(c => c.Name).IsUnique();
            entity.Property(c => c.WebsiteUrl).HasColumnType("text");
            entity.Property(c => c.CompanyType).HasMaxLength(50);
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_companies_company_type",
                "company_type IN ('Multinacional', 'Startup', 'Pyme', 'Consultora')"));
        });
    }
}

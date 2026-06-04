using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Data;

public partial class AppDbContext
{
    private static void ConfigureKeyword(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Keyword>(entity =>
        {
            entity.ToTable("keywords");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Id).ValueGeneratedOnAdd();
            entity.Property(k => k.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(k => k.Name).IsUnique();
            entity.Property(k => k.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
        });
    }
}

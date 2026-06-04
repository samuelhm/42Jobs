using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Data;

public partial class AppDbContext
{
    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", t => t.HasCheckConstraint(
                "CK_users_role",
                "role IN ('Admin', 'User')"));
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id)
                  .HasDefaultValueSql("gen_random_uuid()")
                  .ValueGeneratedOnAdd();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(300);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).HasMaxLength(300);
            entity.Property(u => u.Name).HasMaxLength(200);
            entity.Property(u => u.LastName).HasMaxLength(200);
            entity.Property(u => u.Phone).HasMaxLength(50);
            entity.Property(u => u.Address).HasColumnType("text");
            entity.Property(u => u.LinkedinUrl).HasColumnType("text");
            entity.Property(u => u.WebsiteUrl).HasColumnType("text");
            entity.Property(u => u.GithubUrl).HasColumnType("text");
            entity.Property(u => u.Junior).HasDefaultValue(true);
            entity.Property(u => u.Presentation).HasColumnType("text");
            entity.Property(u => u.AvatarUrl).HasColumnType("text");
            entity.Property(u => u.PreferredLocation).HasMaxLength(200);
            entity.Property(u => u.Role)
                  .HasMaxLength(20)
                  .HasDefaultValue("User");
            entity.Property(u => u.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.Property(u => u.UpdatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAddOrUpdate();
        });
    }
}

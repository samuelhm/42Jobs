using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Data;

public partial class AppDbContext
{
    private static void ConfigureAi(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiService>(entity =>
        {
            entity.ToTable("ai_services");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedOnAdd();
            entity.Property(s => s.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(s => s.Name).IsUnique();
            entity.Property(s => s.ApiKey).HasMaxLength(500);
            entity.Property(s => s.IsActive).HasDefaultValue(true);
            entity.Property(s => s.IsFreeTier).HasDefaultValue(false);
            entity.Property(s => s.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.Property(s => s.UpdatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<AiModel>(entity =>
        {
            entity.ToTable("ai_models");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).ValueGeneratedOnAdd();
            entity.Property(m => m.Name).IsRequired().HasMaxLength(100);
            entity.Property(m => m.IsActive).HasDefaultValue(true);
            entity.Property(m => m.SupportsReasoning).HasDefaultValue(false);
            entity.Property(m => m.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.HasIndex(m => new { m.AiServiceId, m.Name }).IsUnique();
            entity.HasIndex(m => m.AiServiceId).HasDatabaseName("idx_ai_models_service");

            entity.HasOne(m => m.AiService)
                  .WithMany(s => s.Models)
                  .HasForeignKey(m => m.AiServiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiPrompt>(entity =>
        {
            entity.ToTable("ai_prompts");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).ValueGeneratedOnAdd();
            entity.Property(p => p.Functionality).IsRequired().HasMaxLength(100);
            entity.HasIndex(p => p.Functionality).IsUnique();
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasColumnType("text");
            entity.Property(p => p.SystemPrompt).IsRequired().HasColumnType("text");
                entity.Property(p => p.UserPromptTemplate).IsRequired().HasColumnType("text");
            entity.Property(p => p.IsActive).HasDefaultValue(true);
            entity.Property(p => p.UseReasoning).HasDefaultValue(false);
            entity.Property(p => p.ReasoningEffort).HasMaxLength(20);
            entity.Property(p => p.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.Property(p => p.UpdatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAddOrUpdate();

            entity.HasIndex(p => p.DefaultModelId).HasDatabaseName("idx_ai_prompts_model");
            entity.HasOne(p => p.DefaultModel)
                  .WithMany()
                  .HasForeignKey(p => p.DefaultModelId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

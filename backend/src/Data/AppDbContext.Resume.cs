using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Data;

public partial class AppDbContext
{
    private static void ConfigureResume(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Resume>(entity =>
        {
            entity.ToTable("resumes");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id)
                  .HasDefaultValueSql("gen_random_uuid()")
                  .ValueGeneratedOnAdd();
            entity.Property(r => r.CvData)
                  .IsRequired()
                  .HasColumnType("text")
                  .HasDefaultValue("");
            entity.Property(r => r.JsonData).HasColumnType("jsonb");
            entity.Property(r => r.Model)
                  .HasMaxLength(30)
                  .HasDefaultValue("gpt-5.4-mini");
            entity.Property(r => r.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.Property(r => r.UpdatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAddOrUpdate();
            entity.HasIndex(r => new { r.UserId, r.JobId }).IsUnique();
            entity.HasIndex(r => r.UserId).HasDatabaseName("idx_resumes_user");
            entity.HasIndex(r => r.JobId).HasDatabaseName("idx_resumes_job");

            entity.HasOne(r => r.User)
                  .WithMany(u => u.Resumes)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Job)
                  .WithMany(j => j.Resumes)
                  .HasForeignKey(r => r.JobId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(r => r.Template)
                  .WithMany(t => t.Resumes)
                  .HasForeignKey(r => r.TemplateId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(r => r.Prompt)
                  .WithMany(p => p.Resumes)
                  .HasForeignKey(r => r.PromptId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(r => r.AiModel)
                  .WithMany(m => m.Resumes)
                  .HasForeignKey(r => r.ModelId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

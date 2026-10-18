using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Child> Children => Set<Child>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Teacher>(e =>
        {
            e.Property(t => t.DisplayName).HasMaxLength(100);
            e.Property(t => t.Phone).HasMaxLength(20);
            e.Property(t => t.PinHash).HasMaxLength(200);
            e.Property(t => t.GreetingAudioUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<Class>(e =>
        {
            e.Property(c => c.Medium).HasMaxLength(5);
            e.HasOne<Teacher>().WithMany().HasForeignKey(c => c.TeacherId);
        });

        modelBuilder.Entity<Child>(e =>
        {
            e.Property(c => c.FirstName).HasMaxLength(50);
            e.Property(c => c.HomeLanguage).HasMaxLength(5);
            e.Property(c => c.ParentPhone).HasMaxLength(20);
            e.HasOne<Class>().WithMany().HasForeignKey(c => c.ClassId);
            e.HasIndex(c => new { c.ClassId, c.RollNo }).IsUnique();
        });
    }
}

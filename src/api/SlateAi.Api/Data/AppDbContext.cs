using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<Attendance> Attendance => Set<Attendance>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<ProcessedRequest> ProcessedRequests => Set<ProcessedRequest>();
    public DbSet<AiCall> AiCalls => Set<AiCall>();

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

        modelBuilder.Entity<Attendance>(e =>
        {
            e.HasKey(a => new { a.ClassId, a.Date, a.ChildId });
            e.HasOne<Class>().WithMany().HasForeignKey(a => a.ClassId);
            e.HasOne<Child>().WithMany().HasForeignKey(a => a.ChildId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lesson>(e =>
        {
            e.Property(l => l.Source).HasMaxLength(10);
            e.Property(l => l.Status).HasMaxLength(20);
            e.HasOne<Class>().WithMany().HasForeignKey(l => l.ClassId);
            e.HasIndex(l => new { l.ClassId, l.Date });
        });

        modelBuilder.Entity<ProcessedRequest>(e =>
        {
            e.HasKey(r => r.ClientRequestId);
            e.Property(r => r.Endpoint).HasMaxLength(100);
        });

        modelBuilder.Entity<AiCall>(e =>
        {
            e.Property(c => c.Purpose).HasMaxLength(50);
            e.Property(c => c.Model).HasMaxLength(100);
            e.Property(c => c.Verdict).HasMaxLength(100);
            e.Property(c => c.Error).HasMaxLength(300);
            e.HasIndex(c => c.CreatedAt);
        });
    }
}

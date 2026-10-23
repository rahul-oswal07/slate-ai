using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<Attendance> Attendance => Set<Attendance>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<ProcessedRequest> ProcessedRequests => Set<ProcessedRequest>();
    public DbSet<AiCall> AiCalls => Set<AiCall>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<EveningMessage> EveningMessages => Set<EveningMessage>();
    public DbSet<CatchUpPackage> CatchUpPackages => Set<CatchUpPackage>();
    public DbSet<CatchUpAssignment> CatchUpAssignments => Set<CatchUpAssignment>();
    public DbSet<AudioClip> AudioClips => Set<AudioClip>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Teacher>(e =>
        {
            e.Property(t => t.DisplayName).HasMaxLength(100);
            e.Property(t => t.Phone).HasMaxLength(20);
            e.Property(t => t.PinHash).HasMaxLength(200);
            e.HasIndex(t => t.Phone).IsUnique();
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
            e.Property(c => c.Consent).HasMaxLength(10).HasDefaultValue("no");
            e.HasOne<Class>().WithMany().HasForeignKey(c => c.ClassId);
            e.HasIndex(c => new { c.ClassId, c.RollNo }).IsUnique();
        });

        modelBuilder.Entity<Attendance>(e =>
        {
            e.HasKey(a => new { a.ClassId, a.Date, a.ChildId });
            e.Property(a => a.Source).HasMaxLength(10).HasDefaultValue("manual");
            e.HasOne<Class>().WithMany().HasForeignKey(a => a.ClassId);
            e.HasOne<Child>().WithMany().HasForeignKey(a => a.ChildId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lesson>(e =>
        {
            e.Property(l => l.Source).HasMaxLength(10);
            e.Property(l => l.Status).HasMaxLength(20);
            e.HasOne<Class>().WithMany().HasForeignKey(l => l.ClassId);
            e.HasIndex(l => new { l.ClassId, l.Date }).IsUnique(); // one lesson record per day (D-55)
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

        modelBuilder.Entity<MessageTemplate>(e =>
        {
            e.Property(t => t.Language).HasMaxLength(5);
            e.Property(t => t.Version).HasMaxLength(20);
            e.Property(t => t.Text).HasMaxLength(1000);
            e.Property(t => t.PageFormat).HasMaxLength(100);
            e.Property(t => t.PageFormatNoPart).HasMaxLength(100);
            e.Property(t => t.Status).HasMaxLength(10);
            e.HasOne<Class>().WithMany().HasForeignKey(t => t.ClassId);
            e.HasOne<Teacher>().WithMany().HasForeignKey(t => t.ApprovedByTeacherId);
            e.HasIndex(t => new { t.ClassId, t.Language }).IsUnique();
        });

        modelBuilder.Entity<EveningMessage>(e =>
        {
            e.Property(m => m.Language).HasMaxLength(5);
            e.Property(m => m.SlotValues).HasColumnType("jsonb");
            e.Property(m => m.Token).HasMaxLength(30);
            e.HasOne<Class>().WithMany().HasForeignKey(m => m.ClassId);
            e.HasOne<Child>().WithMany().HasForeignKey(m => m.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<MessageTemplate>().WithMany().HasForeignKey(m => m.TemplateId);
            e.HasOne<Lesson>().WithMany().HasForeignKey(m => m.LessonId);
            e.HasIndex(m => new { m.ChildId, m.Date }).IsUnique(); // one message per child per day: the send is idempotent
            e.HasIndex(m => m.Token).IsUnique();
            e.HasIndex(m => new { m.ClassId, m.Date });
        });

        modelBuilder.Entity<CatchUpPackage>(e =>
        {
            e.Property(p => p.TargetKey).HasMaxLength(100);
            e.Property(p => p.Language).HasMaxLength(5);
            e.Property(p => p.Status).HasMaxLength(20);
            e.Property(p => p.Hint).HasMaxLength(20);
            e.Property(p => p.Verdict).HasMaxLength(100);
            e.Property(p => p.Activity).HasColumnType("jsonb");
            e.Property(p => p.CheckItem).HasColumnType("jsonb");
            e.Property(p => p.Review).HasColumnType("jsonb");
            e.Property(p => p.AudioSegments).HasColumnType("jsonb");
            e.Property(p => p.AudioUrl).HasMaxLength(500);
            e.HasOne<Class>().WithMany().HasForeignKey(p => p.ClassId);
            e.HasOne<Teacher>().WithMany().HasForeignKey(p => p.ApprovedByTeacherId);
            e.HasIndex(p => new { p.ClassId, p.WeekOf, p.Language, p.TargetKey }).IsUnique();
        });

        modelBuilder.Entity<CatchUpAssignment>(e =>
        {
            e.Property(a => a.CompressionRationale).HasColumnType("jsonb");
            e.Property(a => a.Token).HasMaxLength(30);
            e.HasOne<Class>().WithMany().HasForeignKey(a => a.ClassId);
            e.HasOne<Child>().WithMany().HasForeignKey(a => a.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<CatchUpPackage>().WithMany().HasForeignKey(a => a.PackageId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => new { a.ChildId, a.WeekOf }).IsUnique(); // one catch-up per child per week
            e.HasIndex(a => a.Token).IsUnique();
            e.HasIndex(a => new { a.ClassId, a.WeekOf });
        });

        modelBuilder.Entity<AudioClip>(e =>
        {
            e.Property(c => c.Kind).HasMaxLength(20);
            e.Property(c => c.Language).HasMaxLength(5);
            e.Property(c => c.TextHash).HasMaxLength(64);
            e.Property(c => c.ContentType).HasMaxLength(50);
            e.HasOne<CatchUpPackage>().WithMany().HasForeignKey(c => c.PackageId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(c => new { c.Kind, c.Language, c.TextHash }).IsUnique().HasFilter("\"TextHash\" IS NOT NULL");
            e.HasIndex(c => c.PackageId);
        });
    }
}

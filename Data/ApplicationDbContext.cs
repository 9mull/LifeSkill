using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LifeSkill.Web.Models;

namespace LifeSkill.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Lesson> Lessons { get; set; } = null!;
    public DbSet<LessonContent> LessonContents { get; set; }
    public DbSet<TestQuestion> TestQuestions { get; set; }
    public DbSet<UserTestResult> UserTestResults { get; set; }
    public DbSet<UserSandboxStats> UserSandboxStats { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<IdentityRole>();
        builder.Ignore<IdentityRoleClaim<string>>();
        builder.Ignore<IdentityUserClaim<string>>();
        builder.Ignore<IdentityUserLogin<string>>();
        builder.Ignore<IdentityUserRole<string>>();
        builder.Ignore<IdentityUserToken<string>>();

        var userEntity = builder.Entity<ApplicationUser>();
        
        userEntity.Ignore(u => u.AccessFailedCount);
        userEntity.Ignore(u => u.ConcurrencyStamp);
        userEntity.Ignore(u => u.LockoutEnabled);
        userEntity.Ignore(u => u.LockoutEnd);
        userEntity.Ignore(u => u.PhoneNumber);
        userEntity.Ignore(u => u.PhoneNumberConfirmed);
        userEntity.Ignore(u => u.SecurityStamp);
        userEntity.Ignore(u => u.TwoFactorEnabled);

        userEntity.Property(u => u.NormalizedEmail).HasMaxLength(256);
        userEntity.Property(u => u.NormalizedUserName).HasMaxLength(256);
        userEntity.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex");
        userEntity.HasIndex(u => u.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();

        builder.Entity<Course>()
            .HasMany(c => c.Lessons)
            .WithOne(l => l.Course)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Course>()
            .HasIndex(c => c.OrderIndex);

        builder.Entity<Lesson>()
            .HasIndex(l => new { l.CourseId, l.OrderIndex });

        builder.Entity<Lesson>()
            .HasOne(l => l.ContentBlock)
            .WithOne(c => c.Lesson)
            .HasForeignKey<LessonContent>(c => c.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 
using Microsoft.EntityFrameworkCore;
using Sena_app.Models;

namespace Sena_app.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Reminder> Reminders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── [User] ───────────────────────────────────────────────────────
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("User");
                e.HasKey(u => u.Id);
                e.Property(u => u.Id).HasColumnName("id_user").ValueGeneratedOnAdd();
                e.Property(u => u.FirstName).HasColumnName("name").HasMaxLength(100).IsRequired();
                e.Property(u => u.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
                e.Property(u => u.Email).HasMaxLength(100).IsRequired();
                e.Property(u => u.Password).HasMaxLength(255).IsRequired();
                e.Property(u => u.Phone).HasColumnName("phone").HasMaxLength(20);
                e.Property(u => u.PrefNotf).HasColumnName("pref_notf").HasMaxLength(50);
                e.Property(u => u.PrefView).HasColumnName("pref_view").HasMaxLength(50);
                e.Property(u => u.CreatedAt).HasColumnName("created_at")
                 .HasDefaultValueSql("GETDATE()");
                e.HasIndex(u => u.Email).IsUnique();
                e.Ignore(u => u.FullName);
                e.Ignore(u => u.Initials);
                e.Ignore(u => u.Plan);
                e.Property(u => u.GoogleAccessToken).HasColumnName("google_access_token");
                e.Property(u => u.GoogleRefreshToken).HasColumnName("google_refresh_token");
                e.Property(u => u.GoogleTokenExpiry).HasColumnName("google_token_expiry");
                e.Ignore(u => u.IsGoogleAccount);
            });

            // ── Category ─────────────────────────────────────────────────────
            modelBuilder.Entity<Category>(e =>
            {
                e.ToTable("Category");
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).HasColumnName("id_category").ValueGeneratedOnAdd();
                e.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
                e.Property(c => c.Description).HasColumnName("description").HasMaxLength(255);
                e.Property(c => c.Color).HasColumnName("color").HasMaxLength(30);
                e.Property(c => c.IdUser).HasColumnName("id_user");

                e.HasOne(c => c.User)
                 .WithMany(u => u.Categories)
                 .HasForeignKey(c => c.IdUser)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Task ─────────────────────────────────────────────────────────
            modelBuilder.Entity<TaskItem>(e =>
            {
                e.ToTable("Task");
                e.HasKey(t => t.Id);
                e.Property(t => t.Id).HasColumnName("id_task").ValueGeneratedOnAdd();
                e.Property(t => t.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
                e.Property(t => t.Description).HasColumnName("description");
                e.Property(t => t.LimitDate).HasColumnName("limit_date");
                e.Property(t => t.LimitHour).HasColumnName("limit_hour");
                e.Property(t => t.Priority)
                 .HasColumnName("priority")
                 .HasConversion<string>()
                 .HasMaxLength(10);
                e.Property(t => t.Status)
                 .HasColumnName("state")
                 .HasConversion<string>()
                 .HasMaxLength(10);
                e.Property(t => t.IdUser).HasColumnName("id_user");
                e.Property(t => t.IdCategory).HasColumnName("id_category");
                e.Property(t => t.CreatedAt).HasColumnName("created_at")
                 .HasDefaultValueSql("GETDATE()");
                e.Property(t => t.GoogleEventId).HasColumnName("google_event_id");

                // Ignorar propiedades calculadas — no van a la BD
                e.Ignore(t => t.DueDate);
                e.Ignore(t => t.Category);
                e.Ignore(t => t.UserId);
                e.Ignore(t => t.IsDone);
                e.Ignore(t => t.IsOverdue);
                e.Ignore(t => t.IsDueToday);

                e.HasOne(t => t.User)
                 .WithMany(u => u.Tasks)
                 .HasForeignKey(t => t.IdUser)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(t => t.CategoryNav)
                 .WithMany(c => c.Tasks)
                 .HasForeignKey(t => t.IdCategory)
                 .OnDelete(DeleteBehavior.NoAction);
            });

            // ── Reminder ─────────────────────────────────────────────────────
            modelBuilder.Entity<Reminder>(e =>
            {
                e.ToTable("Reminder");
                e.HasKey(r => r.Id);
                e.Property(r => r.Id).HasColumnName("id_reminder").ValueGeneratedOnAdd();
                e.Property(r => r.SendDate).HasColumnName("send_date").IsRequired();
                e.Property(r => r.SendHour).HasColumnName("send_hour").IsRequired();
                e.Property(r => r.TypeNotf)
                 .HasColumnName("type_notf")
                 .HasConversion<string>()
                 .HasMaxLength(10);
                e.Property(r => r.Frequency)
                 .HasColumnName("frequency")
                 .HasConversion<string>()
                 .HasMaxLength(10);
                e.Property(r => r.State)
                 .HasColumnName("state")
                 .HasConversion<string>()
                 .HasMaxLength(15);
                e.Property(r => r.MinutePost).HasColumnName("minute_post").HasDefaultValue(0);
                e.Property(r => r.IdTask).HasColumnName("id_task");
                e.HasIndex(r => r.IdTask).IsUnique();

                e.HasOne(r => r.Task)
                 .WithOne(t => t.Reminder)
                 .HasForeignKey<Reminder>(r => r.IdTask)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Otzivi.Models;

namespace Otzivi.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ReviewLike> ReviewLikes { get; set; }

        // 🔐 ДОБАВЬТЕ ЭТУ СТРОКУ
        public DbSet<SecurityEvent> SecurityEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Brand).IsRequired().HasMaxLength(50);
                entity.Property(p => p.Description).HasMaxLength(500);
                entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            });

            builder.Entity<Review>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Title).IsRequired().HasMaxLength(100);
                entity.Property(r => r.Content).IsRequired().HasMaxLength(1000);

                entity.HasOne(r => r.Product)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(r => r.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                    .WithMany(u => u.Reviews)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ReviewLike>(entity =>
            {
                entity.HasKey(rl => rl.Id);

                entity.HasIndex(rl => new { rl.ReviewId, rl.UserId }).IsUnique();

                entity.HasOne(rl => rl.Review)
                    .WithMany(r => r.ReviewLikes)
                    .HasForeignKey(rl => rl.ReviewId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rl => rl.User)
                    .WithMany(u => u.ReviewLikes)
                    .HasForeignKey(rl => rl.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 🔐 ДОБАВЬТЕ ЭТОТ БЛОК
            builder.Entity<SecurityEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.UserAgent).HasMaxLength(200);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.SecurityEvents) // 👈 ЭТА СТРОКА ССЫЛАЕТСЯ НА СВОЙСТВО В ApplicationUser
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
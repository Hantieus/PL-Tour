using Microsoft.EntityFrameworkCore;
using PLTour.Shared.Models.Entities;

namespace PLTour.API.Models.DbContext
{
    public class PLTourDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public PLTourDbContext(DbContextOptions<PLTourDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Narration> Narrations { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<VendorImage> VendorImages { get; set; }

        // THÊM 2 DÒNG NÀY CHO TOUR
        public DbSet<Tour> Tours { get; set; }
        public DbSet<TourLocation> TourLocations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Seed dữ liệu cho Languages
            modelBuilder.Entity<Language>().HasData(
                new Language { LanguageId = 1, Name = "Tiếng Việt", Code = "vi", FlagIcon = "flag-icon-vn", DisplayOrder = 1, IsActive = true },
                new Language { LanguageId = 2, Name = "English", Code = "en", FlagIcon = "flag-icon-us", DisplayOrder = 2, IsActive = true },
                new Language { LanguageId = 3, Name = "中文", Code = "zh", FlagIcon = "flag-icon-cn", DisplayOrder = 3, IsActive = true },
                new Language { LanguageId = 4, Name = "한국어", Code = "ko", FlagIcon = "flag-icon-kr", DisplayOrder = 4, IsActive = true },
                new Language { LanguageId = 5, Name = "日本語", Code = "ja", FlagIcon = "flag-icon-jp", DisplayOrder = 5, IsActive = true }
            );

            // 2. Cấu hình Narration
            modelBuilder.Entity<Narration>()
                .HasOne(n => n.Location)
                .WithMany(l => l.Narrations)
                .HasForeignKey(n => n.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Narration>()
                .HasOne(n => n.Language)
                .WithMany()
                .HasForeignKey(n => n.LanguageId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Narration>()
                .HasIndex(n => new { n.LocationId, n.IsDefault })
                .IsUnique()
                .HasFilter("\"IsDefault\" = true");

            // 3. Seed dữ liệu Category
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "Điểm tham quan", Description = "Các điểm tham quan, di tích lịch sử...", Icon = "fa-landmark", DisplayOrder = 1, IsActive = true },
                new Category { CategoryId = 2, Name = "Địa điểm ăn uống", Description = "Nhà hàng, quán ăn...", Icon = "fa-utensils", DisplayOrder = 2, IsActive = true },
                new Category { CategoryId = 3, Name = "Sự kiện", Description = "Các sự kiện, lễ hội...", Icon = "fa-calendar-alt", DisplayOrder = 3, IsActive = true }
            );

            // 4. Seed dữ liệu admin
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Username = "admin",
                    Email = "admin@pltour.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    FullName = "Administrator",
                    Phone = "0123456789",
                    Role = "Admin",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                }
            );

            // 5. Cấu hình các mối quan hệ hiện có
            modelBuilder.Entity<Location>()
                .HasOne(l => l.Category)
                .WithMany(c => c.Locations)
                .HasForeignKey(l => l.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vendor>()
                .HasOne(v => v.Category)
                .WithMany(c => c.Vendors)
                .HasForeignKey(v => v.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Vendor)
                .WithMany(v => v.Products)
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VendorImage>()
                .HasOne(vi => vi.Vendor)
                .WithMany()
                .HasForeignKey(vi => vi.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- PHẦN QUAN TRỌNG: CẤU HÌNH CHO TOUR & TOUR_LOCATION ---

            // Tạo khóa chính kép cho bảng trung gian
            modelBuilder.Entity<TourLocation>()
                .HasKey(tl => new { tl.TourId, tl.LocationId });

            // Quan hệ TourLocation -> Tour
            modelBuilder.Entity<TourLocation>()
                .HasOne(tl => tl.Tour)
                .WithMany(t => t.TourLocations)
                .HasForeignKey(tl => tl.TourId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ TourLocation -> Location
            modelBuilder.Entity<TourLocation>()
                .HasOne(tl => tl.Location)
                .WithMany()
                .HasForeignKey(tl => tl.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
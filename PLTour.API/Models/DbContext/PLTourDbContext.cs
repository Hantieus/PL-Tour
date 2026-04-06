using Microsoft.EntityFrameworkCore;
using PLTour.Shared.Models;

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
        public DbSet<Narration> Narrations { get; set; } //Update đa ngôn ngữ
        public DbSet<Category> Categories { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<Product> Products { get; set; } // THÊM DÒNG NÀY
        public DbSet<Language> Languages { get; set; } // THÊM MỚI

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed dữ liệu cho Languages
            modelBuilder.Entity<Language>().HasData(
                new Language { LanguageId = 1, Name = "Tiếng Việt", Code = "vi", FlagIcon = "flag-icon-vn", DisplayOrder = 1, IsActive = true },
                new Language { LanguageId = 2, Name = "English", Code = "en", FlagIcon = "flag-icon-us", DisplayOrder = 2, IsActive = true },
                new Language { LanguageId = 3, Name = "中文", Code = "zh", FlagIcon = "flag-icon-cn", DisplayOrder = 3, IsActive = true },
                new Language { LanguageId = 4, Name = "한국어", Code = "ko", FlagIcon = "flag-icon-kr", DisplayOrder = 4, IsActive = true },
                new Language { LanguageId = 5, Name = "日本語", Code = "ja", FlagIcon = "flag-icon-jp", DisplayOrder = 5, IsActive = true }
            );

            // Cấu hình Narration
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

            // Mỗi location chỉ có 1 ngôn ngữ mặc định
            modelBuilder.Entity<Narration>()
                .HasIndex(n => new { n.LocationId, n.IsDefault })
                .IsUnique()
                .HasFilter("IsDefault = 1");

            // SỬA LẠI PHẦN NÀY - thêm Description cho mỗi category
            modelBuilder.Entity<Category>().HasData(
        new Category
        {
            CategoryId = 1,
            Name = "Điểm tham quan",
            Description = "Các điểm tham quan, di tích lịch sử, bảo tàng, công viên...",
            Icon = "fa-landmark",
            DisplayOrder = 1,
            IsActive = true
        },
        new Category
        {
            CategoryId = 2,
            Name = "Địa điểm ăn uống",
            Description = "Nhà hàng, quán ăn, đồ ăn đường phố, quán cà phê...",
            Icon = "fa-utensils",
            DisplayOrder = 2,
            IsActive = true
        },
        new Category
        {
            CategoryId = 3,
            Name = "Sự kiện",
            Description = "Các sự kiện, lễ hội, hoạt động đặc biệt",
            Icon = "fa-calendar-alt",
            DisplayOrder = 3,
            IsActive = true
        }
    );


            // Seed dữ liệu admin mặc định
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
                    CreatedDate = DateTime.Now
                }
            );

            // Cấu hình mối quan hệ
            modelBuilder.Entity<Location>()
                .HasOne(l => l.Category)
                .WithMany(c => c.Locations)
                .HasForeignKey(l => l.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Narration>()
                .HasOne(n => n.Location)
                .WithMany(l => l.Narrations)
                .HasForeignKey(n => n.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Vendor>()
                .HasOne(v => v.Category)
                .WithMany(c => c.Vendors)
                .HasForeignKey(v => v.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // THÊM CẤU HÌNH CHO PRODUCT
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Vendor)
                .WithMany(v => v.Products)
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany() // Category không cần có ICollection<Product> nếu không muốn
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides; // Thêm thư viện này
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders; // Thêm thư viện này
using PLTour.Admin.Services;
using PLTour.API.Models.DbContext;
using PLTour.API.Services;
using PLTour.Shared.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Cấu hình DbContext, có retry logic để đảm bảo kết nối ổn định 
//Retry logic
builder.Services.AddDbContext<PLTourDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        }));


// Cấu hình TranslationService
builder.Services.AddScoped<ITranslationService, FreeTranslationService>();

//Cloudinary
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// TTS service
builder.Services.AddHttpClient<ITtsService, EdgeTtsService>();

// Cấu hình Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "AdminAuth";
    })
    .AddCookie("VendorAuth", options =>
    {
        options.LoginPath = "/vendor-login/login";
        options.LogoutPath = "/vendor-login/logout";
        options.AccessDeniedPath = "/vendor-login/accessdenied";
        options.Cookie.Name = "VendorAuth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// --- BẮT ĐẦU PHẦN SỬA ĐỔI QUAN TRỌNG ---

// 1. Cấu hình Forwarded Headers để chạy tốt trên Dev Tunnels/Proxy
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 2. Cấu hình Static Files tường minh hơn
// Đảm bảo phục vụ file trong wwwroot
app.UseStaticFiles();

// Phục vụ riêng thư mục uploads (đề phòng trường hợp wwwroot bị giới hạn)
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// --- KẾT THÚC PHẦN SỬA ĐỔI ---

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Tạo database nếu chưa có và tự động bổ sung các cột mới cho Vendors
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PLTourDbContext>();

    dbContext.Database.EnsureCreated();
    dbContext.Database.ExecuteSqlRaw(@"
        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""BusinessName"" character varying(200) NOT NULL DEFAULT '';

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""ContactName"" character varying(100) NOT NULL DEFAULT '';

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""Email"" character varying(255) NOT NULL DEFAULT '';

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""Phone"" character varying(20) NOT NULL DEFAULT '';

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""PasswordHash"" text NOT NULL DEFAULT '';

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""Status"" character varying(50) NOT NULL DEFAULT 'Pending';

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""IsActive"" boolean NOT NULL DEFAULT false;

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""Plan"" character varying(30) NOT NULL DEFAULT 'Free';

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""PlanExpiresAt"" timestamp with time zone NULL;

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""AvatarUrl"" character varying(200) NULL;

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""Notes"" character varying(500) NULL;

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""Address"" character varying(500) NULL;

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""Description"" character varying(1000) NULL;

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""LogoUrl"" character varying(500) NULL;

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""Latitude"" double precision NULL;

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""Longitude"" double precision NULL;

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""CategoryId"" integer NULL;

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""CreatedDate"" timestamp with time zone NOT NULL DEFAULT NOW();

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""UpdatedDate"" timestamp with time zone NULL;

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""ApprovedDate"" timestamp with time zone NULL;

        UPDATE ""Vendors""
        SET ""BusinessName"" = COALESCE(""BusinessName"", ""ShopName"", '')
        WHERE ""BusinessName"" = '';

        UPDATE ""Vendors""
        SET ""ContactName"" = COALESCE(""ContactName"", ""OwnerName"", '')
        WHERE ""ContactName"" = '';

        ALTER TABLE ""Vendors"" DROP COLUMN IF EXISTS ""ShopName"";
        ALTER TABLE ""Vendors"" DROP COLUMN IF EXISTS ""OwnerName"";

        UPDATE ""Vendors""
        SET ""Plan"" = 'Free'
        WHERE ""Plan"" IS NULL OR ""Plan"" = '';
    ");
}
catch (Exception ex)
{
    Console.WriteLine($"Database init skipped: {ex.Message}");
}

app.Run();

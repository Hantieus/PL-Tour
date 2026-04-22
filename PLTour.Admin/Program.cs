using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides; // Thêm thư viện này
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders; // Thêm thư viện này
using PLTour.Admin.Services;
using PLTour.API.Models.DbContext;
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

// Tạo database nếu chưa có
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PLTourDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();
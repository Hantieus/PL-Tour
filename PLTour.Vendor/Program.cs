using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Services;
using PLTour.Vendor.Services;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký DbContext dùng chung PostgreSQL/Neon với API và Admin
builder.Services.AddDbContext<PLTourDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        }));

// Cloudinary
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.Configure<MomoSettings>(builder.Configuration.GetSection("Momo"));
builder.Services.AddHttpClient<IMomoService, MomoService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Cấu hình Authentication cho Vendor
builder.Services.AddAuthentication("VendorAuth")
    .AddCookie("VendorAuth", options =>
    {
        options.LoginPath = "/vendor-login/login";
        options.LogoutPath = "/vendor-login/logout";
        options.AccessDeniedPath = "/vendor-login/accessdenied";
        options.Cookie.Name = "VendorAuth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=VendorLogin}/{action=Login}/{id?}");
app.MapControllerRoute(
    name: "vendorRegistration",
    pattern: "vendor-registration/{action=Index}/{id?}",
    defaults: new { controller = "VendorRegistration" });
// Route cho Vendor Dashboard
app.MapControllerRoute(
    name: "vendorDashboard",
    pattern: "vendor/dashboard",
    defaults: new { controller = "VendorDashboard", action = "Index" });
app.MapControllerRoute(
    name: "vendorStore",
    pattern: "vendor-store/{action=Index}/{id?}",
    defaults: new { controller = "VendorStore" });
app.MapControllerRoute(
    name: "vendorChangePassword",
    pattern: "vendor/changepassword",
    defaults: new { controller = "VendorDashboard", action = "ChangePassword" });

app.MapControllerRoute(
    name: "vendorProfile",
    pattern: "vendor/profile",
    defaults: new { controller = "VendorDashboard", action = "EditProfile" });

app.MapControllerRoute(
    name: "vendorProducts",
    pattern: "vendor-products/{action=Index}/{id?}",
    defaults: new { controller = "VendorProduct" });

app.MapControllerRoute(
    name: "vendorImages",
    pattern: "vendor/gallery/{action=Index}/{id?}",
    defaults: new { controller = "VendorImage" });

app.MapControllerRoute(
    name: "vendorSubscription",
    pattern: "vendor-subscription/{action=Index}/{id?}",
    defaults: new { controller = "VendorSubscription" });

app.MapControllerRoute(
    name: "vendorLogout",
    pattern: "vendor-login/logout",
    defaults: new { controller = "VendorLogin", action = "Logout" });

try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PLTourDbContext>();

    dbContext.Database.ExecuteSqlRaw(@"
        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""BusinessName"" character varying(200) NOT NULL DEFAULT '';

        ALTER TABLE ""Vendors""
        ADD COLUMN IF NOT EXISTS ""ContactName"" character varying(100) NOT NULL DEFAULT '';

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
    ");
}
catch (Exception ex)
{
    Console.WriteLine($"Vendor DB init skipped: {ex.Message}");
}

app.Run();

using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;

var builder = WebApplication.CreateBuilder(args);
// Đăng ký DBcontext
builder.Services.AddDbContext<PLTourDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    name: "vendorChangePassword",
    pattern: "vendor/changepassword",
    defaults: new { controller = "VendorDashboard", action = "ChangePassword" });

app.MapControllerRoute(
    name: "vendorLogout",
    pattern: "vendor-login/logout",
    defaults: new { controller = "VendorLogin", action = "Logout" });
app.Run();

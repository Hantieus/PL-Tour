using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CẤU HÌNH ĐỂ LẮNG NGHE TỪ ĐIỆN THOẠI THẬT (IP BẤT KỲ) ---
builder.WebHost.ConfigureKestrel(options =>
{
    // Lắng nghe trên cổng 5229 cho tất cả các IP (điện thoại thật gọi vào được)
    options.ListenAnyIP(5229);
    // Nếu bạn vẫn muốn dùng HTTPS trên máy tính thì thêm dòng dưới (tùy chọn)
    options.ListenAnyIP(7291, listenOptions => listenOptions.UseHttps());
});

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PL-Tour API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Nhập token: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Database Context
builder.Services.AddDbContext<PLTourDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// JWT Authentication
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? "PLTourSecretKey2024");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// LƯU Ý: Khi test với điện thoại thật qua HTTP, bạn có thể tạm tắt HttpsRedirection 
// nếu gặp lỗi chặn chứng chỉ không hợp lệ.
// app.UseHttpsRedirection(); 

app.UseRouting(); // Thêm routing rõ ràng

// CORS PHẢI NẰM TRƯỚC Authentication/Authorization
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles(); // Cho phép truy cập file trong thư mục wwwroot

app.MapControllers();


app.Run();
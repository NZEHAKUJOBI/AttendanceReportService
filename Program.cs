using System.Text;
using AttendanceReportService.BackgroundJobs;
using AttendanceReportService.Data;
using AttendanceReportService.Models;
using AttendanceReportService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configure QuestPDF License (Community License for development/non-commercial use)
QuestPDF.Settings.License = LicenseType.Community;

// ✅ Register a system font (Arial) to avoid blank text when no default fonts are available (e.g., IIS)
try
{
    var fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
    var arialPath = Path.Combine(fontsDir, "arial.ttf");
    if (File.Exists(arialPath))
    {
        using var fs = File.OpenRead(arialPath);
        FontManager.RegisterFont(fs);
        Console.WriteLine("✅ QuestPDF font registered: Arial");
    }
    else
    {
        Console.WriteLine($"⚠️ Arial font not found at: {arialPath}. QuestPDF will try system defaults.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Failed to register Arial font for QuestPDF: {ex.Message}");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// JWT Auth
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            ),
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
    );
});
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<DeviceHealthService>();
builder.Services.AddScoped<HealthMonitorService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations(); // ✅ Enables SwaggerOperation, SwaggerResponse, etc.

    // ✅ Add JWT support to Swagger UI
    options.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter 'Bearer' followed by your token.",
        }
    );

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();

    if (!context.Users.Any(u => u.Role == UserRole.Admin))
    {
        var hasher = new PasswordHasher<User>();
        var admin = new User
        {
            FullName = "System Administrator",
            Email = "admin@attendanceservice.com",
            PasswordHash = hasher.HashPassword(null, "Admin@123"),
            Role = UserRole.Admin,
        };

        context.Users.Add(admin);
        context.SaveChanges();
        Console.WriteLine("✅ Admin user seeded successfully.");
    }
}

app.Run();

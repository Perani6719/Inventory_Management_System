using Hangfire;
using Hangfire.SqlServer;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ShelfSense.Application.DTOs.Configuration;
using ShelfSense.Application.Interfaces;
using ShelfSense.Application.Mapping;
using ShelfSense.Application.Services;
using ShelfSense.Application.Services.Auth;
using ShelfSense.Application.Settings;
using ShelfSense.Domain.Identity;
using ShelfSense.Infrastructure.Data;
using ShelfSense.Infrastructure.Repositories;
using ShelfSense.Infrastructure.Seeders;
using ShelfSense.Infrastructure.Services;
using Serilog;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using QuestPDF.Infrastructure;
using ShelfSense.Domain.Entities;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ===================================================
// 🚨 SERILOG INTEGRATION
// ===================================================
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
);

// ===================================================
// SERVICES CONFIGURATION
// ===================================================

// DbContext
builder.Services.AddDbContext<ShelfSenseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ShelfSenseDbContext>()
    .AddDefaultTokenProviders();

// Repositories and Services
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IStoreRepository, StoreRepository>();
builder.Services.AddScoped<IShelfRepository, ShelfRepository>();
builder.Services.AddScoped<IProductShelfRepository, ProductShelfRepository>();
builder.Services.AddScoped<IReplenishmentAlert, ReplenishmentAlertRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<IRestockTaskRepository, RestockTaskRepository>();
builder.Services.AddScoped<IStockRequest, StockRequestRepository>();
builder.Services.AddScoped<ISalesHistory, SalesHistoryRepository>();
builder.Services.AddScoped<IDeliveryStatusLog, DeliveryStatusLogRepository>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<AlertingService>();
builder.Services.AddScoped<IPasswordHasher<Staff>, PasswordHasher<Staff>>();

// Email
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

// Hangfire
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

//Blob Services
builder.Services.AddSingleton<BlobStorageService>();


// Suppress automatic model validation
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// JWT Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<JwtSettings>>().Value);

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),

        // ✅ These two lines are essential
        NameClaimType = ClaimTypes.Name,
        //RoleClaimType = ClaimTypes.Role
        //RoleClaimType = "https://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    };
});


builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ShelfSense.WebAPI", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Enter JWT token like: Bearer {your token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("authPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// ✅ CORS for Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


var app = builder.Build();

// ===================================================
// MIDDLEWARE PIPELINE
// ===================================================

var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Seed Roles and Default User
using (var scope = app.Services.CreateScope())
{
    var sP = scope.ServiceProvider;
    var roleManager = sP.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = sP.GetRequiredService<UserManager<ApplicationUser>>();

    logger.LogInformation("Starting database seeding: roles and default user creation.");

    try
    {
        await UserAndRoleSeeder.SeedRolesAndDefaultUserAsync(roleManager, userManager);
        logger.LogInformation("Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database seeding.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    logger.LogInformation("Swagger UI enabled for Development environment.");
}

// Custom 403 middleware
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 403)
    {
        logger.LogWarning("Access denied (403 Forbidden) for path: {Path}", context.Request.Path);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"message\": \"Access denied. You do not have permission to perform this action.\"}");
    }
});

app.UseHangfireDashboard();
logger.LogInformation("Hangfire Dashboard enabled.");

// Schedule recurring alert job
using (var scope = app.Services.CreateScope())
{
    var alertingService = scope.ServiceProvider.GetRequiredService<AlertingService>();

    const string jobId = "stock-alert-notification";
    const string cronExpression = "0 * * * *"; // Every hour

    RecurringJob.AddOrUpdate(
        jobId,
        () => alertingService.CheckAndNotifyAlerts(),
        cronExpression
    );

    logger.LogInformation("Scheduled Hangfire recurring job: {JobId} with CRON: {Cron}", jobId, cronExpression);
}

app.UseRateLimiter();
logger.LogInformation("Rate Limiter middleware added.");

//app.UseCors("AllowAngularClient");
app.UseCors("AllowAngularDev");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

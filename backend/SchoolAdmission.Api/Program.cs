using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Application.Options;
using SchoolAdmission.Application.Services;
using SchoolAdmission.Domain.Common;
using SchoolAdmission.Infrastructure.Common;
using SchoolAdmission.Infrastructure.Data;
using SchoolAdmission.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Program).Assembly)
    .AddJsonOptions(options =>
{
options.JsonSerializerOptions.PropertyNamingPolicy =
    JsonNamingPolicy.CamelCase;

options.JsonSerializerOptions.DefaultIgnoreCondition =
    JsonIgnoreCondition.Never;

options.JsonSerializerOptions.Converters.Add(
    new JsonStringEnumConverter());
});

// Configure Admission Options
builder.Services.Configure<AdmissionOptions>(options =>
{
var holdDurationEnv =
    Environment.GetEnvironmentVariable("HOLD_DURATION_SECONDS");

if (!string.IsNullOrWhiteSpace(holdDurationEnv) &&
    int.TryParse(holdDurationEnv, out var duration))
{
options.HoldDurationSeconds = duration;
}
else if (builder.Configuration.GetValue<int?>(
             "HOLD_DURATION_SECONDS") is { } cfgDuration)
{
options.HoldDurationSeconds = cfgDuration;
}
else
{
options.HoldDurationSeconds = 600;
}
});

// Configure PostgreSQL Database
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "PostgreSQL connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
options.UseNpgsql(connectionString);
});

// Register Domain & Application Services
builder.Services.AddSingleton<ISystemClock, SystemClock>();

builder.Services.AddScoped<IClassService, ClassService>();

builder.Services.AddScoped<IHoldService, HoldService>();

// Configure CORS
builder.Services.AddCors(options =>
{
options.AddPolicy("AllowAll", policy =>
{
policy.AllowAnyOrigin()
      .AllowAnyMethod()
      .AllowAnyHeader()
      .WithExposedHeaders("X-Student-Id");
});
});

// Swagger
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
app.UseSwagger();
app.UseSwaggerUI();
}

// Global Exception Handler Middleware
app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Apply PostgreSQL migrations on startup
try
{
using var scope = app.Services.CreateScope();

var db = scope.ServiceProvider
    .GetRequiredService<ApplicationDbContext>();

db.Database.Migrate();
}
catch (Exception ex)
{
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogError(
    ex,
    "Could not apply PostgreSQL database migrations on startup.");

throw;
}

app.Run();

public partial class Program { }
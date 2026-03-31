using API.Infrastructure.Data;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Database — EF Core with SQL Server
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------------------------------------------------------------------
// Controllers + Swagger / OpenAPI
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Obi Bridge API",
        Version = "v1",
        Description = "ESG data integration — API-to-SFTP bridge"
    });
});

// ---------------------------------------------------------------------------
// FluentValidation
// ---------------------------------------------------------------------------
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ---------------------------------------------------------------------------
// Hangfire (uncomment when SQL connection string is configured)
// ---------------------------------------------------------------------------
// builder.Services.AddHangfire(config =>
//     config.UseSqlServerStorage(
//         builder.Configuration.GetConnectionString("HangfireConnection")));
// builder.Services.AddHangfireServer();

// ---------------------------------------------------------------------------
// Azure AD Authentication (uncomment when Azure AD is configured)
// ---------------------------------------------------------------------------
// builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

// ---------------------------------------------------------------------------
// CORS
// ---------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000" })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ---------------------------------------------------------------------------
// Application services (register here as they are created)
// ---------------------------------------------------------------------------
// builder.Services.AddScoped<ISyncService, SyncService>();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

// app.UseAuthentication();   // Enable when Azure AD is configured
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make the implicit Program class public so test projects can reference it
public partial class Program { }

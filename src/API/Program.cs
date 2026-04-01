using System.Threading.RateLimiting;
using API.Application.Auth;
using API.Application.Services;
using API.Core.Interfaces;
using API.Infrastructure.Data;
using API.Infrastructure.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Database — EF Core (SQL Server in production, InMemory for development)
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("ObiBridgeDev"));
}

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
// Authentication & Authorization
// ---------------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthorizationHandler, ConnectionAccessHandler>();

// Azure AD Authentication — enable when AzureAd config section is present
var azureAdSection = builder.Configuration.GetSection("AzureAd");
if (azureAdSection.Exists())
{
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
}
else
{
    // Development fallback — accept any request as authenticated Admin user
    builder.Services.AddAuthentication("DevScheme")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, API.Infrastructure.DevAuthHandler>(
            "DevScheme", _ => { });
}

builder.Services.AddAuthorization(options =>
{
    AuthorizationPolicies.ConfigurePolicies(options);
});

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
                ?? new[] { "http://localhost:3000", "http://localhost:5173" })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ---------------------------------------------------------------------------
// Application services (register here as they are created)
// ---------------------------------------------------------------------------
// builder.Services.AddScoped<ISyncService, SyncService>();

// Repositories
builder.Services.AddScoped<IConnectionRepository, ConnectionRepository>();
builder.Services.AddScoped<IConnectionCredentialRepository, ConnectionCredentialRepository>();
builder.Services.AddScoped<ISyncRunRepository, SyncRunRepository>();
builder.Services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
builder.Services.AddScoped<INotificationConfigRepository, NotificationConfigRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// HTTP client for API connector
builder.Services.AddHttpClient("ApiConnector", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "ObiBridge/1.0");
});
builder.Services.AddHttpClient("OAuth2");

// Core services
builder.Services.AddScoped<IApiConnectorService, ApiConnectorService>();
builder.Services.AddScoped<ICredentialVaultService, CredentialVaultService>();
builder.Services.AddScoped<ITransformEngineService, TransformEngineService>();
builder.Services.AddScoped<ICsvGeneratorService, CsvGeneratorService>();
builder.Services.AddScoped<ISftpDeliveryService, SftpDeliveryService>();
builder.Services.AddScoped<ISyncOrchestratorService, SyncOrchestratorService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IValidationEngineService, ValidationEngineService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Azure Key Vault — conditional on config
var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Services.AddSingleton<IKeyVaultClient>(
        new API.Infrastructure.KeyVaultClientWrapper(
            new Azure.Security.KeyVault.Secrets.SecretClient(
                new Uri(keyVaultUri), new Azure.Identity.DefaultAzureCredential())));
}
else
{
    // Development: no-op Key Vault client
    builder.Services.AddSingleton<IKeyVaultClient, API.Infrastructure.DevKeyVaultClient>();
}

// Scheduler — Hangfire in production, no-op in development
if (!string.IsNullOrEmpty(connectionString))
{
    // Hangfire needs SQL Server — register when connection string is available
    // builder.Services.AddHangfire(config => config.UseSqlServerStorage(connectionString));
    // builder.Services.AddHangfireServer();
    // builder.Services.AddScoped<ISchedulerService, SchedulerService>();
    builder.Services.AddScoped<ISchedulerService, API.Infrastructure.DevSchedulerService>();
}
else
{
    builder.Services.AddScoped<ISchedulerService, API.Infrastructure.DevSchedulerService>();
}

// ---------------------------------------------------------------------------
// Rate Limiting
// ---------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 20
            }));
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------------------

// Global exception handler — prevent internal details from leaking in responses
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new { success = false, message = "An unexpected error occurred." };
        await context.Response.WriteAsJsonAsync(response);
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make the implicit Program class public so test projects can reference it
public partial class Program { }

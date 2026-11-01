using System.Text;
using EmployeeManagement.API.Health;
using EmployeeManagement.API.Middlewares;
using EmployeeManagement.Application.Common;
using EmployeeManagement.Domain.Constants;
using EmployeeManagement.Infrastructure;
using EmployeeManagement.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ─────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings section is missing from configuration.");

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// Clean Architecture: register Infrastructure & Application services
builder.Services.AddInfrastructure(builder.Configuration);

// ── Authentication & Authorization ────────────────────────────────────────────
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminOnly, policy =>
        policy.RequireRole(Roles.Admin));

    options.AddPolicy(Policies.CanManageEmployees, policy =>
        policy.RequireRole(Roles.Admin, Roles.HrManager));

    options.AddPolicy(Policies.CanReadEmployees, policy =>
        policy.RequireRole(Roles.Admin, Roles.HrManager, Roles.Viewer));
});

// ── Pipeline (Ordered Middleware Chain) ───────────────────────────────────────
var app = builder.Build();

// 1. Correlation ID: Attach trace ID to logs and response headers
app.UseRequestCorrelation();

// 2. Security Headers: Add OWASP security headers to all responses
app.UseCustomSecurityHeaders();

// 3. Exception Handling: Global catch-all returning RFC 7807 ProblemDetails
app.UseGlobalExceptionHandler();

// 4. Performance Profiling: Measure execution duration and log slow requests
app.UseRequestProfiling();

// 5. Rate Limiting: Token Bucket algorithm per IP / user
app.UseTokenBucketRateLimiting();

// 6. HTTPS Redirection & Static Files (for uploaded photos)
app.UseHttpsRedirection();
app.UseStaticFiles();

// 7. Security: Authentication followed by Authorization
app.UseAuthentication();
app.UseAuthorization();

// 8. Health Check Endpoints (Orchestrator Probes)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});

// 9. Endpoints
app.MapControllers();

// Initialize/seed database if reachable
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<EmployeeManagement.Infrastructure.Persistence.DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.Run();

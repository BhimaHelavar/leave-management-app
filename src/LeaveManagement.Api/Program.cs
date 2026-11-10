using LeaveManagement.Application;
using LeaveManagement.Infrastructure;
using LeaveManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Auth:Mode is the single switch between "Dev" (local tokens, Phase 4c) and "EntraId" (later).
// Business code never reads it; only the authentication setup does.
var authMode = builder.Configuration["Auth:Mode"] ?? "Dev";
if (authMode is not ("Dev" or "EntraId"))
{
    throw new InvalidOperationException(
        $"Auth:Mode must be 'Dev' or 'EntraId', but was '{authMode}'.");
}

// CORS: allow-list of exactly one origin (the Angular app). No wildcard.
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:4200";

builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
    options.AddPolicy("web", policy => policy
        .WithOrigins(allowedOrigin)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// "Now" always comes from TimeProvider so tests can control it (golden rule).
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Migrations and demo data run at startup in Development only (3b decision).
// Elsewhere, migrations run as a separate step with a separate database login.
if (app.Environment.IsDevelopment())
{
    await DatabaseInitializer.InitializeAsync(app.Services, seedDemoData: true);
}

// Errors become ProblemDetails responses with no stack traces.
app.UseExceptionHandler();
app.UseStatusCodePages();

// Baseline security headers. A stricter Content-Security-Policy is set on the web server (nginx).
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    await next();
});

app.UseCors("web");

// The fallback "require authenticated user" policy is added in Phase 4c together with
// the authentication scheme. Until then /health is the only endpoint.
app.MapGet("/health", async (LeaveDbContext db, TimeProvider time, CancellationToken ct) =>
    {
        var databaseUp = await db.Database.CanConnectAsync(ct);
        return Results.Ok(new HealthResponse(
            Status: databaseUp ? "Healthy" : "Degraded",
            AuthMode: authMode,
            Database: databaseUp ? "Connected" : "Unavailable",
            Environment: app.Environment.EnvironmentName,
            TimeUtc: time.GetUtcNow()));
    })
    .AllowAnonymous()
    .WithName("Health");

app.Run();

internal sealed record HealthResponse(
    string Status, string AuthMode, string Database, string Environment, DateTimeOffset TimeUtc);

// Makes the entry point visible to integration tests (WebApplicationFactory) in Phase 5.
public partial class Program;

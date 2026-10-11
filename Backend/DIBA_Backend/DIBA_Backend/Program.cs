using DIBA_Backend.Data;
using DIBA_Backend.Health;
using DIBA_Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Apply a per-client-IP limit to expensive anonymous authentication attempts.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddHttpClient<YocoPaymentService>();
builder.Services.AddScoped<NotificationEmailService>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadinessHealthCheck>(
        "database",
        tags: new[] { "ready" });

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be configured as a secret containing at least 32 UTF-8 bytes.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
}

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "JWT Demo API",
        Version = "v1",
        Description = "A simple JWT Authentication API"
    });

    // Reference: Microsoft/OpenAPI documentation and the reviewed
    // ASP.NET Core authentication project by Olaide Ogunbunmi.
    // Similar logic: defining a Bearer authentication scheme in Swagger
    // so that authenticated endpoints can be tested with a JWT.
    // DIBA adaptation: Swagger is configured with the Bearer scheme
    // used by the DIBA API.
    // https://github.com/olaideogunbunmi/aspnetcore-auth-rbac
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "JWT Authorization header using the Bearer scheme. " +
            "Example: \"Authorization: Bearer {token}\"",

        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });


    // Similar Swagger security requirement logic:
    // tells Swagger that the Bearer token should be used when
    // calling protected API endpoints.
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
    }
    );
});


builder.Services.AddDbContext<DIBABookingsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DbConn")));


// Reference: Microsoft Learn, "Configure JWT bearer authentication
// in ASP.NET Core".
//
// Similar logic:
// 1. Register JWT Bearer as the authentication scheme.
// 2. Configure token validation.
// 3. Validate the issuer.
// 4. Validate the audience.
// 5. Validate the token lifetime.
// 6. Validate the signing key.
//
// DIBA adaptation: issuer, audience and signing key are read from
// the DIBA configuration file.
// https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication
builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey!))
            };
    });


// Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
// Similar logic: authorization services are registered so that
// authenticated users can be checked against their roles and claims.
// DIBA uses this together with [Authorize] and [Authorize(Roles = "...")]
// throughout the controllers.
// https://learn.microsoft.com/aspnet/core/security/authorization/roles
builder.Services.AddAuthorization();

var app = builder.Build();

// Return safe ProblemDetails responses instead of exposing exception details.
app.UseExceptionHandler();


// Demo accounts and sample records must only be created in Development.
// Production migrations are applied explicitly during deployment.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DIBABookingsDbContext>();
    //await DbSeeder.SeedAsync(dbContext);
}

app.UseRouting();
app.UseCors("MyPolicy");
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


// Reference: Microsoft Learn, "Configure JWT bearer authentication
// in ASP.NET Core".
//
// Similar middleware order:
// UseAuthentication() processes the supplied authentication credentials
// and establishes the user's identity before authorization is evaluated.
app.UseAuthentication();


// Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
//
// Similar middleware:
// UseAuthorization() applies authorization rules after the user has
// been authenticated.
app.UseAuthorization();

app.MapControllers();
// Liveness only confirms that the API process is responding.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness includes the database dependency check. The existing /health
// endpoint is retained for compatibility and reports all registered checks.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health");

app.Run();
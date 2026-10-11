using DIBA_Backend.Data;
using DIBA_Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Fail early when required JWT settings are missing or unsafe.
// Keep the signing key in User Secrets during development or in a secure
// environment variable (Jwt__Key) outside development; never commit it.
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key is missing or too short. Configure Jwt:Key using " +
        "User Secrets or the Jwt__Key environment variable with at least 32 bytes.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT issuer and audience must both be configured.");
}

builder.Services.AddHttpClient<YocoPaymentService>();

// Allow only the local frontend origins used during development.
// Add the actual frontend origin here if its host or port changes.
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", policy =>
        policy.WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://127.0.0.1:3000",
                "https://127.0.0.1:3000")
              .AllowAnyMethod()
              .AllowAnyHeader());
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
    // calling protected endpoints.
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

builder.Services.AddDbContext<DIBABookingsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DbConn")));

// Reference: Microsoft Learn, "Configure JWT bearer authentication
// in ASP.NET Core".
// Similar logic: register JWT Bearer and validate issuer, audience,
// token lifetime, and signing key.
// DIBA adaptation: values are read from DIBA configuration.
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

                ValidIssuer =
                    jwtIssuer,

                ValidAudience =
                    jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtKey))
            };
    });

// Role-based authorization is applied by the controllers using
// [Authorize] and [Authorize(Roles = "...")].
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("MyPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

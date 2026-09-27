using DIBA_Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", builder =>
        builder.AllowAnyOrigin()
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

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["Jwt:Key"]))
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


// DIBA-specific database initialization.
// The application creates a service scope and obtains the database
// context before seeding the required initial roles/statuses/test data.
using (var scope = app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<DIBABookingsDbContext>();

    await DbSeeder.SeedAsync(dbContext);
}

app.UseCors("MyPolicy");

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

app.Run();

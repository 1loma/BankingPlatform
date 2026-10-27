using BankingPlatform.Api.Data;
using BankingPlatform.Api.Models.Identity;
using BankingPlatform.Api.Services;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using System.Text;

namespace BankingPlatform.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register controllers
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Represent enum values as "Checking", not 0
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();

            // Configure Swagger with JWT authorization
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT access token"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                        Array.Empty<string>()
                    }
                });
            });

            // Connect EF Core to SQL Server
            builder.Services.AddDbContext<BankingDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("BankingDatabase")));

            builder.Services.AddDataProtection();

            // Configure ASP.NET Core Identity
            builder.Services
                .AddIdentityCore<ApplicationUser>(options =>
                {
                    // Require email verification before login
                    options.SignIn.RequireConfirmedEmail = true;

                    // Prevent duplicate email addresses
                    options.User.RequireUniqueEmail = true;

                    // Lock account for 15 minutes after 5 failed attempts
                    options.Lockout.DefaultLockoutTimeSpan =
                        TimeSpan.FromMinutes(15);

                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<BankingDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();
            
            // Password reset and email confirmation tokens expire in 1 hour
            builder.Services.Configure<DataProtectionTokenProviderOptions>(
                options => { options.TokenLifespan = TimeSpan.FromHours(1); });

            // Register application services
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<JwtService>();

            // Read JWT secret from User Secrets
            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT key is missing.");

            if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                throw new InvalidOperationException(
                    "JWT key must be at least 32 bytes.");
            }

            // Configure JWT authentication
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // Keep JWT claim names unchanged
                    options.MapInboundClaims = false;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // Verify the server that created the token
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],

                        // Verify the intended audience
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["Jwt:Audience"],

                        // Reject expired tokens
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,

                        // Verify the token's digital signature
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey))
                    };
                });

            // Enable authorization
            builder.Services.AddAuthorization();



            var app = builder.Build();

            // Enable Swagger in development
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Validate user identity before checking permissions
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
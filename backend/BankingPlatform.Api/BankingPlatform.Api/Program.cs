using BankingPlatform.Api.Data;
using BankingPlatform.Api.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BankingPlatform.Api.Services;

namespace BankingPlatform.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();


            // Enable Swagger in development
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            builder.Services.AddDbContext<BankingDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("BankingDatabase")));

            builder.Services.AddDataProtection();

            builder.Services.AddIdentityCore<ApplicationUser>(options =>
                {
                    options.SignIn.RequireConfirmedEmail = true;
                    options.User.RequireUniqueEmail = true;
                }).AddRoles<IdentityRole>()
                  .AddEntityFrameworkStores<BankingDbContext>()
                  .AddSignInManager()
                  .AddDefaultTokenProviders();

            // One EmailService instance per request scope
            builder.Services.AddScoped<EmailService>();


            var app = builder.Build();
            
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();

                // Enable Swagger in development
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}

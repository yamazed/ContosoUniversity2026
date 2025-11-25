
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ContosoUniversity.Data;
using ContosoUniversity.Middleware;
using ContosoUniversity.Filters;

namespace ContosoUniversity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllersWithViews(options =>
            {
                // Add global model validation filter
                options.Filters.Add<ValidateModelStateFilter>();
            });

            // Configure CORS for React application
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ReactApp", policy =>
                {
                    // Build list of allowed origins
                    var allowedOrigins = new List<string>
                    {
                        "http://localhost:5173",  // Vite dev server default port
                        "http://localhost:3000",  // Alternative dev port
                        "http://localhost:4173"   // Vite preview port
                    };

                    // Add CloudFront origin from environment variable if provided
                    var cloudFrontOrigin = builder.Configuration["CORS:AllowedOrigins"];
                    if (!string.IsNullOrEmpty(cloudFrontOrigin))
                    {
                        allowedOrigins.Add(cloudFrontOrigin);
                    }

                    policy.WithOrigins(allowedOrigins.ToArray())
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            // Register HttpClient for NotificationService
            builder.Services.AddHttpClient();

            // Register NotificationService
            builder.Services.AddScoped<ContosoUniversity.Services.NotificationService>();

            // Configure Entity Framework Core with dependency injection
            // Try to get connection string from configuration first (for local development)
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // If not found, construct from environment variables (for AWS deployment)
            if (string.IsNullOrEmpty(connectionString))
            {
                // Read directly from environment variables (not through Configuration)
                var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
                var dbName = Environment.GetEnvironmentVariable("DB_NAME");
                var dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME");
                var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

                Console.WriteLine($"DB_HOST from env: {dbHost ?? "NULL"}");
                Console.WriteLine($"DB_NAME from env: {dbName ?? "NULL"}");
                Console.WriteLine($"DB_USERNAME from env: {dbUsername ?? "NULL"}");
                Console.WriteLine($"DB_PASSWORD from env: {(string.IsNullOrEmpty(dbPassword) ? "NULL" : "SET")}");

                if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbName) &&
                    !string.IsNullOrEmpty(dbUsername) && !string.IsNullOrEmpty(dbPassword))
                {
                    // Use NpgsqlConnectionStringBuilder to properly escape special characters
                    var connBuilder = new Npgsql.NpgsqlConnectionStringBuilder
                    {
                        Host = dbHost,
                        Database = dbName,
                        Username = dbUsername,
                        Password = dbPassword
                    };
                    connectionString = connBuilder.ConnectionString;
                    Console.WriteLine("Using connection string from environment variables");
                    Console.WriteLine($"Connection string length: {connectionString.Length}");
                }
                else
                {
                    throw new InvalidOperationException(
                        "Database configuration not found. Please set DB_HOST, DB_NAME, DB_USERNAME, and DB_PASSWORD environment variables " +
                        "or provide a ConnectionString in appsettings.json");
                }
            }
            else
            {
                Console.WriteLine("Using connection string from appsettings.json");
            }

            // Register SchoolContext with DbContext options
            // Configure Npgsql to use timestamp without time zone for DateTime
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            
            builder.Services.AddDbContext<SchoolContext>(options =>
                options.UseNpgsql(connectionString));

            // Configure Kestrel to listen on port 80 (for AWS deployment) or 5000 (for local dev)
            // The ASPNETCORE_URLS environment variable will be read automatically
            builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 10485760;
            });

            var app = builder.Build();

            // Initialize database and seed data
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<SchoolContext>();
                if (context != null)
                {
                    DbInitializer.Initialize(context);
                }
            }

            // Configure the HTTP request pipeline
            
            // Use global exception handler middleware for all environments
            app.UseGlobalExceptionHandler();

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Enable CORS
            app.UseCors("ReactApp");

            app.UseAuthorization();

            // Map API controllers with /api prefix
            app.MapControllers();

            // Map MVC controllers with default route
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}

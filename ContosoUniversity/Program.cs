
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
                    policy.WithOrigins(
                        "http://localhost:5173",  // Vite dev server default port
                        "http://localhost:3000",  // Alternative dev port
                        "http://localhost:4173",  // Vite preview port
                        "https://contoso-university.netlify.app",  // Production URL (example)
                        "https://contoso-university.vercel.app"    // Production URL (example)
                    )
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
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // Register SchoolContext with DbContext options
            // Configure Npgsql to use timestamp without time zone for DateTime
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            
            builder.Services.AddDbContext<SchoolContext>(options =>
                options.UseNpgsql(connectionString));

            // Configure Kestrel
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

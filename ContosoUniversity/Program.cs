
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ContosoUniversity.Data;
using System.Data.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;

namespace ContosoUniversity
{
    public class AppSettings
    {
        public string NotificationQueuePath { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add configuration sources
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            // Add services to the container
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            // Add session support
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Configure database with connection string from web.config
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                "Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=ContosoUniversityNoAuthEFCore;Integrated Security=True;MultipleActiveResultSets=True";
            builder.Services.AddScoped<SchoolContext>(provider =>
            {
                var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<SchoolContext>();
                Microsoft.EntityFrameworkCore.SqlServerDbContextOptionsExtensions.UseSqlServer(optionsBuilder, connectionString);
                return new SchoolContext(optionsBuilder.Options);
            });

            // Configure Kestrel - Setting max request size from web.config maxRequestLength (10240KB) and maxAllowedContentLength
            builder.WebHost.UseKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 10485760; // 10MB in bytes
            });

            // Add Newtonsoft.Json for MVC
            builder.Services.AddControllersWithViews()
                .AddNewtonsoftJson();

            // Add configuration for NotificationQueuePath from web.config appSettings
            builder.Services.Configure<AppSettings>(options =>
            {
                options.NotificationQueuePath = builder.Configuration["NotificationQueuePath"] ?? ".\\Private$\\ContosoUniversityNotifications";
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            // Configure MVC options for client validation (from web.config appSettings)
            app.Services.GetRequiredService<IOptions<MvcViewOptions>>().Value.HtmlHelperOptions.ClientValidationEnabled = true;

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            // Initialize database
using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<SchoolContext>();
                    DbInitializer.Initialize(context);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while initializing the database.");
                }
            }

            app.Run();
        }
    }
}

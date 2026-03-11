
    using System;
using System.Collections.Generic;
using System.Data.Entity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ContosoUniversity.Data;

    namespace ContosoUniversity
    {
        public class Program
        {
            public static void Main(string[] args)
            {
                var builder = WebApplication.CreateBuilder(args);

                // Store configuration in static ConfigurationManager
                ConfigurationManager.Configuration = builder.Configuration;

                // Configure request size limits and timeouts
                builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
                {
                    options.MultipartBodyLengthLimit = 10485760; // 10MB (maxAllowedContentLength from Web.config)
                });

                builder.Services.Configure<IISServerOptions>(options =>
                {
                    options.MaxRequestBodySize = 10485760; // 10MB
                });

                builder.WebHost.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.Limits.MaxRequestBodySize = 10485760; // 10MB (maxRequestLength from Web.config)
                    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(3600); // executionTimeout from Web.config
                });

                // Add services to the container (formerly ConfigureServices)
                builder.Services.AddControllersWithViews();

                // Register SchoolContext
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                builder.Services.AddScoped<SchoolContext>(provider =>
                {
                    var optionsBuilder = new DbContextOptionsBuilder<SchoolContext>();
                    optionsBuilder.UseSqlServer(connectionString);
                    return new SchoolContext(optionsBuilder.Options);
                });

                //Added Services

                var app = builder.Build();

                // Initialize database with Entity Framework 6
using (var scope = app.Services.CreateScope())
                {
                    var dbConnectionString = app.Configuration.GetConnectionString("DefaultConnection");
using (var context = new SchoolContext(new DbContextOptionsBuilder<SchoolContext>().UseSqlServer(dbConnectionString).Options))
                    {
                        DbInitializer.Initialize(context);
                    }
                }

                // Configure the HTTP request pipeline (formerly Configure method)
                if (app.Environment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseExceptionHandler("/Home/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();

                //Added Middleware

                app.UseRouting();

                app.UseAuthorization();

                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                app.Run();
            }
        }

        public class ConfigurationManager
        {
            public static IConfiguration Configuration { get; set; }
        }
    }

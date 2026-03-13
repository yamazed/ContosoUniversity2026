using Microsoft.EntityFrameworkCore;
using NotificationAPI.Data;
using NotificationAPI.Repositories;
using NotificationAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure database connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Fallback to environment variables if connection string not in config
if (string.IsNullOrEmpty(connectionString))
{
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
    var dbName = Environment.GetEnvironmentVariable("DB_NAME");
    var dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME");
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
    
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
    }
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string is not configured. " +
        "Please provide a connection string in appsettings.json under 'ConnectionStrings:DefaultConnection' " +
        "or set environment variables: DB_HOST, DB_NAME, DB_USERNAME, DB_PASSWORD");
}

// Enable legacy timestamp behavior for PostgreSQL (same as ContosoUniversity)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Register DbContext with PostgreSQL
builder.Services.AddDbContext<NotificationContext>(options =>
    options.UseNpgsql(connectionString));

// Register repository
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// Register NotificationService as scoped (required for repository injection)
builder.Services.AddScoped<NotificationService>();

// Configure CORS to allow requests from ContosoUniversity
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowContosoUniversity", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Validate database connection on startup
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<NotificationContext>();
        var canConnect = context.Database.CanConnect();
        if (canConnect)
        {
            app.Logger.LogInformation("Database connection validated successfully.");
        }
        else
        {
            app.Logger.LogWarning("Cannot connect to the database. The application will start but database operations may fail.");
        }
    }
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Failed to validate database connection. The application will start but database operations may fail.");
}

// Configure the HTTP request pipeline.
// Enable Swagger in all environments for demo/testing
app.UseSwagger();
app.UseSwaggerUI();

// Enable CORS
app.UseCors("AllowContosoUniversity");

app.UseAuthorization();

// Configure controllers and routing
app.MapControllers();

app.Run();

// Make Program class accessible to test project
public partial class Program { }

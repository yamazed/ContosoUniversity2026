using NotificationAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register NotificationService as singleton
builder.Services.AddSingleton<NotificationService>();

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

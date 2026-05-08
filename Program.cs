using Microsoft.EntityFrameworkCore;
using ProductRepositoryPattern.Data;
using ProductRepositoryPattern.Repositories.Implementations;
using ProductRepositoryPattern.Repositories.Interfaces;
using ProductRepositoryPattern.Services.Implementations;
using ProductRepositoryPattern.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Explicitly log environment variables for debugging
Console.WriteLine("=== STARTUP DEBUGGING ===");
Console.WriteLine($"DATABASE_URL exists: {Environment.GetEnvironmentVariable("DATABASE_URL") != null}");
var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (dbUrl != null)
{
    Console.WriteLine($"DATABASE_URL length: {dbUrl.Length}");
    Console.WriteLine($"DATABASE_URL prefix: {dbUrl.Substring(0, Math.Min(50, dbUrl.Length))}...");
}
else
{
    Console.WriteLine("DATABASE_URL is NULL - using appsettings fallback");
}

// Parse Railway's DATABASE_URL
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    Console.WriteLine("Parsing DATABASE_URL...");
    try
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');

        // Try different connection string formats

        // Format 1: Standard Npgsql format
        var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};" +
                              $"Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

        Console.WriteLine($"Using connection string to host: {uri.Host}:{uri.Port}, database: {uri.LocalPath.TrimStart('/')}");

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.EnableSensitiveDataLogging(); // Helps debugging
            options.EnableDetailedErrors();
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR parsing DATABASE_URL: {ex.Message}");
        throw;
    }
}
else
{
    Console.WriteLine("WARNING: Using local connection string fallback");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("PsqlConnectionString")));
}

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI();
app.UseDeveloperExceptionPage();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Enhanced Auto-migrate with explicit logging
Console.WriteLine("=== ATTEMPTING DATABASE MIGRATION ===");
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Console.WriteLine("Testing database connection...");
        var canConnect = dbContext.Database.CanConnect();
        Console.WriteLine($"Database can connect: {canConnect}");

        if (!canConnect)
        {
            Console.WriteLine("Cannot connect to database - check connection string");
            // Try to get more info about pending migrations
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            Console.WriteLine($"Pending migrations: {string.Join(", ", pendingMigrations)}");
        }

        Console.WriteLine("Applying migrations...");
        dbContext.Database.Migrate();
        Console.WriteLine("✅ Migrations applied successfully!");

        // Verify table exists
        var productsExist = dbContext.Database.ExecuteSqlRaw(
            "SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'Products')");
        Console.WriteLine($"Products table exists check completed");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ MIGRATION FAILED: {ex.GetType().Name}");
        Console.WriteLine($"Message: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");

        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }

        // Don't throw - let the app start but log the error
        Console.WriteLine("WARNING: Continuing despite migration failure");
    }
}
Console.WriteLine("=== STARTUP COMPLETE ===");

app.Run();
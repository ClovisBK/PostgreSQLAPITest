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

// Parse Railway's DATABASE_URL
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};" +
                          $"Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;TrustServerCertificate=true";

    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
}
else
{
    // Local development fallback
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

// Enhanced Auto-migrate with force option
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Console.WriteLine("=== CHECKING DATABASE STATE ===");

        // Check if migration history exists but tables are missing
        var hasMigrationHistory = dbContext.Database.ExecuteSqlRaw(
            "SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = '__EFMigrationsHistory')") > 0;

        Console.WriteLine($"Migration history table exists: {hasMigrationHistory}");

        // Check if Products table exists
        var productsExist = dbContext.Database.ExecuteSqlRaw(
            "SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'Products')") > 0;

        Console.WriteLine($"Products table exists: {productsExist}");

        if (hasMigrationHistory && !productsExist)
        {
            Console.WriteLine("⚠️ INCONSISTENT STATE DETECTED: Migration history exists but Products table is missing!");
            Console.WriteLine("Attempting to fix by removing migration history and recreating...");

            // Drop the migration history table
            dbContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS \"__EFMigrationsHistory\"");
            Console.WriteLine("Dropped migration history table");

            // Now run migrations fresh
            Console.WriteLine("Applying fresh migrations...");
            dbContext.Database.Migrate();
            Console.WriteLine("✅ Fresh migrations applied successfully!");
        }
        else if (!productsExist)
        {
            Console.WriteLine("No tables exist. Applying migrations...");
            dbContext.Database.Migrate();
            Console.WriteLine("✅ Migrations applied successfully!");
        }
        else
        {
            Console.WriteLine("✅ Database is consistent and up to date");
        }

        // Final verification
        var finalCheck = dbContext.Database.ExecuteSqlRaw(
            "SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'Products')") > 0;
        Console.WriteLine($"Final verification - Products table exists: {finalCheck}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ MIGRATION FAILED: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");

        // Last resort: Force create everything
        Console.WriteLine("Attempting EnsureCreated as last resort...");
        dbContext.Database.EnsureCreated();
        Console.WriteLine("EnsureCreated completed");
    }
}

app.Run();
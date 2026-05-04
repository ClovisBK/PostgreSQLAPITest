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

// Helper function to get connection string
string GetConnectionString()
{
    Console.WriteLine("=== STARTING CONNECTION STRING DEBUG ===");

    // List ALL environment variables to see what's available
    Console.WriteLine("All environment variables:");
    foreach (System.Collections.DictionaryEntry env in Environment.GetEnvironmentVariables())
    {
        if (env.Key.ToString()!.Contains("DATABASE") ||
            env.Key.ToString()!.Contains("POSTGRES") ||
            env.Key.ToString()!.Contains("PG"))
        {
            Console.WriteLine($"  {env.Key} = {env.Value}");
        }
    }

    // Try to get DATABASE_URL
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    Console.WriteLine($"DATABASE_URL value: '{databaseUrl}'");
    Console.WriteLine($"DATABASE_URL is null or empty: {string.IsNullOrEmpty(databaseUrl)}");

    if (string.IsNullOrEmpty(databaseUrl))
    {
        Console.WriteLine("WARNING: DATABASE_URL not found! Trying alternative names...");

        // Try alternative names
        databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL_PUBLIC");
        if (!string.IsNullOrEmpty(databaseUrl)) Console.WriteLine($"Found DATABASE_URL_PUBLIC");

        if (string.IsNullOrEmpty(databaseUrl))
            databaseUrl = Environment.GetEnvironmentVariable("POSTGRES_URL");
        if (!string.IsNullOrEmpty(databaseUrl)) Console.WriteLine($"Found POSTGRES_URL");

        if (string.IsNullOrEmpty(databaseUrl))
            databaseUrl = Environment.GetEnvironmentVariable("DB_URL");
        if (!string.IsNullOrEmpty(databaseUrl)) Console.WriteLine($"Found DB_URL");
    }

    if (string.IsNullOrEmpty(databaseUrl))
    {
        Console.WriteLine("ERROR: No database URL found in environment variables!");
        Console.WriteLine("Falling back to appsettings.json...");
        return builder.Configuration.GetConnectionString("PsqlConnectionString")!;
    }

    Console.WriteLine($"Raw DATABASE_URL: {databaseUrl}");

    // Parse the URL
    try
    {
        var uri = new Uri(databaseUrl);
        Console.WriteLine($"Parsed URI - Host: {uri.Host}, Port: {uri.Port}, Path: {uri.LocalPath}");

        var userInfo = uri.UserInfo.Split(':');
        Console.WriteLine($"Username: {userInfo[0]}, Password length: {userInfo[1].Length}");

        var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SslMode=Require;TrustServerCertificate=true";

        Console.WriteLine($"Created connection string: {connectionString}");
        return connectionString;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error parsing DATABASE_URL: {ex.Message}");
        throw;
    }
}

// Get connection string
var connectionString = GetConnectionString();
Console.WriteLine($"Using connection string (password hidden): {connectionString.Substring(0, connectionString.IndexOf("Password=") + 9)}***");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDeveloperExceptionPage();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Run migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Console.WriteLine("Attempting to connect to database...");
        db.Database.Migrate();
        Console.WriteLine("✅ Database migration completed successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Migration failed: {ex.Message}");
        throw;
    }
}

app.Run();
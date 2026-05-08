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

// Force recreate database from your model
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Console.WriteLine("Forcing database recreation...");
    dbContext.Database.EnsureDeleted(); // WARNING: This deletes all data!
    dbContext.Database.EnsureCreated();
    Console.WriteLine("Database recreated with all tables");
}

app.Run();
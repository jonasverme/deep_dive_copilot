using System.Text.Json.Serialization;
using CarInventory.Api.Data;
using CarInventory.Api.Endpoints;
using CarInventory.Api.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────────────────────────

builder.Services.AddDbContext<CarInventoryDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=carinventory.db"));

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title   = "Car Inventory API",
        Version = "v1",
        Description = "ASP.NET Core 8 · Minimal API · SQLite"
    });
});

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Car Inventory v1");
    c.RoutePrefix = "swagger";
});

// ── Endpoints ─────────────────────────────────────────────────────────────────
app.MapCarEndpoints();
app.MapOwnerEndpoints();

// ── Database init ─────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CarInventoryDbContext>();
    db.Database.EnsureCreated();
    SeedData.Apply(db);
}

app.Run();

// Expose Program for integration tests
public partial class Program { }

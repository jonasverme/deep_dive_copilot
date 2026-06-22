using CarInventory.Api.Data;
using CarInventory.Api.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarInventory.Api.Endpoints;

public static class CarEndpoints
{
    public static IEndpointRouteBuilder MapCarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cars").WithTags("Cars");

        // GET /api/cars  — list with optional filters
        group.MapGet("/", async (
            CarInventoryDbContext db,
            string? make,
            string? status,
            int? minYear,
            int? maxYear) =>
        {
            var query = db.Cars.Include(c => c.Owner).AsQueryable();

            if (!string.IsNullOrWhiteSpace(make))   query = query.Where(c => c.Make == make);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
            if (minYear.HasValue)                   query = query.Where(c => c.Year >= minYear.Value);
            if (maxYear.HasValue)                   query = query.Where(c => c.Year <= maxYear.Value);

            var cars = await query
                .OrderBy(c => c.Make).ThenBy(c => c.Model)
                .Select(c => new
                {
                    c.Id, c.Make, c.Model, c.Year, c.Color,
                    c.Mileage, c.Price, c.Status, c.Vin,
                    Owner = c.Owner == null ? null : new { c.Owner.Id, c.Owner.FullName, c.Owner.Email }
                })
                .ToListAsync();

            return Results.Ok(cars);
        })
        .WithSummary("List all cars with optional filters");

        // GET /api/cars/{id}  — single car with service history
        group.MapGet("/{id:int}", async (int id, CarInventoryDbContext db) =>
        {
            var car = await db.Cars
                .Include(c => c.Owner)
                .Include(c => c.ServiceRecords.OrderByDescending(s => s.ServiceDate))
                .FirstOrDefaultAsync(c => c.Id == id);

            return car is null ? Results.NotFound() : Results.Ok(car);
        })
        .WithSummary("Get a single car with full service history");

        // POST /api/cars
        group.MapPost("/", async (
            Car car,
            CarInventoryDbContext db,
            IValidator<Car> validator) =>
        {
            var result = await validator.ValidateAsync(car);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            db.Cars.Add(car);
            await db.SaveChangesAsync();
            return Results.Created($"/api/cars/{car.Id}", car);
        })
        .WithSummary("Add a new car");

        // PUT /api/cars/{id}
        group.MapPut("/{id:int}", async (
            int id,
            Car updated,
            CarInventoryDbContext db,
            IValidator<Car> validator) =>
        {
            var car = await db.Cars.FindAsync(id);
            if (car is null) return Results.NotFound();

            var result = await validator.ValidateAsync(updated);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            car.Make    = updated.Make;
            car.Model   = updated.Model;
            car.Year    = updated.Year;
            car.Color   = updated.Color;
            car.Mileage = updated.Mileage;
            car.Price   = updated.Price;
            car.Status  = updated.Status;
            car.OwnerId = updated.OwnerId;

            await db.SaveChangesAsync();
            return Results.Ok(car);
        })
        .WithSummary("Update a car");

        // DELETE /api/cars/{id}
        group.MapDelete("/{id:int}", async (int id, CarInventoryDbContext db) =>
        {
            var car = await db.Cars.FindAsync(id);
            if (car is null) return Results.NotFound();

            db.Cars.Remove(car);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithSummary("Delete a car");

        return app;
    }
}

// ── Validator ────────────────────────────────────────────────────────────────
public class CarValidator : AbstractValidator<Car>
{
    public CarValidator()
    {
        RuleFor(c => c.Make).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Model).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 2);
        RuleFor(c => c.Vin).NotEmpty().Length(17);
        RuleFor(c => c.Price).GreaterThan(0);
        RuleFor(c => c.Mileage).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Status).Must(s => s is "Available" or "Sold" or "Reserved")
            .WithMessage("Status must be Available, Sold, or Reserved.");
    }
}

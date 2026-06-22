using CarInventory.Api.Data;
using CarInventory.Api.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarInventory.Api.Endpoints;

public static class OwnerEndpoints
{
    public static IEndpointRouteBuilder MapOwnerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/owners").WithTags("Owners");

        // GET /api/owners
        group.MapGet("/", async (CarInventoryDbContext db) =>
        {
            var owners = await db.Owners
                .Include(o => o.Cars)
                .Select(o => new
                {
                    o.Id, o.FirstName, o.LastName, o.Email, o.Phone,
                    CarCount = o.Cars.Count
                })
                .OrderBy(o => o.LastName)
                .ToListAsync();

            return Results.Ok(owners);
        })
        .WithSummary("List all owners");

        // GET /api/owners/{id}
        group.MapGet("/{id:int}", async (int id, CarInventoryDbContext db) =>
        {
            var owner = await db.Owners
                .Include(o => o.Cars)
                .FirstOrDefaultAsync(o => o.Id == id);

            return owner is null ? Results.NotFound() : Results.Ok(owner);
        })
        .WithSummary("Get an owner with their cars");

        // POST /api/owners
        group.MapPost("/", async (
            Owner owner,
            CarInventoryDbContext db,
            IValidator<Owner> validator) =>
        {
            var result = await validator.ValidateAsync(owner);
            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            db.Owners.Add(owner);
            await db.SaveChangesAsync();
            return Results.Created($"/api/owners/{owner.Id}", owner);
        })
        .WithSummary("Register a new owner");

        // DELETE /api/owners/{id}
        group.MapDelete("/{id:int}", async (int id, CarInventoryDbContext db) =>
        {
            var owner = await db.Owners.FindAsync(id);
            if (owner is null) return Results.NotFound();

            db.Owners.Remove(owner);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithSummary("Remove an owner");

        return app;
    }
}

// ── Validator ────────────────────────────────────────────────────────────────
public class OwnerValidator : AbstractValidator<Owner>
{
    public OwnerValidator()
    {
        RuleFor(o => o.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(o => o.LastName).NotEmpty().MaximumLength(50);
        RuleFor(o => o.Email).NotEmpty().EmailAddress().MaximumLength(100);
        RuleFor(o => o.Phone).MaximumLength(20);
    }
}

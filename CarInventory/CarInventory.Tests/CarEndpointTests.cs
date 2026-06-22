extern alias ApiAssembly;
using Program = ApiAssembly::Program;

using ApiAssembly::CarInventory.Api.Data;
using ApiAssembly::CarInventory.Api.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarInventory.Tests;

public class CarEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CarEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.Single(d =>
                    d.ServiceType == typeof(DbContextOptions<CarInventoryDbContext>));
                services.Remove(descriptor);

                services.AddDbContext<CarInventoryDbContext>(opt =>
                    opt.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GET_Cars_Returns200AndEmptyList()
    {
        var response = await _client.GetAsync("/api/cars");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cars = await response.Content.ReadFromJsonAsync<List<object>>();
        cars.Should().NotBeNull();
    }

    [Fact]
    public async Task GET_SwaggerJson_Returns200()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_Car_WithValidData_Returns201()
    {
        var car = new Car
        {
            Make    = "BMW",
            Model   = "320i",
            Year    = 2023,
            Vin     = "WBA5A5C51FD520001",
            Color   = "Black",
            Mileage = 5000,
            Price   = 35000m,
            Status  = "Available"
        };

        var response = await _client.PostAsJsonAsync("/api/cars", car);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_Car_WithInvalidVin_Returns400()
    {
        var car = new Car
        {
            Make    = "BMW",
            Model   = "320i",
            Year    = 2023,
            Vin     = "TOO-SHORT",
            Color   = "Black",
            Mileage = 5000,
            Price   = 35000m,
            Status  = "Available"
        };

        var response = await _client.PostAsJsonAsync("/api/cars", car);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Car_WithInvalidStatus_Returns400()
    {
        var car = new Car
        {
            Make    = "BMW",
            Model   = "320i",
            Year    = 2023,
            Vin     = "WBA5A5C51FD520002",
            Color   = "Black",
            Mileage = 0,
            Price   = 35000m,
            Status  = "Unknown"
        };

        var response = await _client.PostAsJsonAsync("/api/cars", car);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Car_ById_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync("/api/cars/9999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Car_Returns204()
    {
        var car = new Car
        {
            Make = "Audi", Model = "A4", Year = 2022,
            Vin = "WAUZZZ8K5LA098001", Color = "Silver",
            Mileage = 20000, Price = 25000m, Status = "Available"
        };
        var createResp = await _client.PostAsJsonAsync("/api/cars", car);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResp.Content.ReadFromJsonAsync<Car>();
        created.Should().NotBeNull();

        var deleteResp = await _client.DeleteAsync($"/api/cars/{created!.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await _client.GetAsync($"/api/cars/{created.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

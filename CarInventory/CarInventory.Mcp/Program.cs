using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddSingleton(new HttpClient { BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/") })
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

[McpServerToolType]
public static class CarInventoryTools
{
    [McpServerTool(Name = "list_cars"), Description("Returns all cars in the inventory with make, color, model, year, price, status and owner.")]
    public static async Task<string> ListCars(HttpClient http)
        => await http.GetStringAsync("api/cars");

    [McpServerTool(Name = "search_cars"), Description("Search cars by make, status, or year range.")]
    public static async Task<string> SearchCars(
        HttpClient http,
        [Description("Car brand, e.g. BMW")] string? make = null,
        [Description("One of: Available, Sold, Reserved")] string? status = null,
        [Description("Minimum year")] int? minYear = null,
        [Description("Maximum year")] int? maxYear = null)
    {
        var sb  = new StringBuilder("api/cars");
        var sep = '?';

        if (!string.IsNullOrWhiteSpace(make))
        { sb.Append($"{sep}make={Uri.EscapeDataString(make)}");     sep = '&'; }
        if (!string.IsNullOrWhiteSpace(status))
        { sb.Append($"{sep}status={Uri.EscapeDataString(status)}"); sep = '&'; }
        if (minYear.HasValue)
        { sb.Append($"{sep}minYear={minYear}");                     sep = '&'; }
        if (maxYear.HasValue)
        { sb.Append($"{sep}maxYear={maxYear}");                     sep = '&'; }

        return await http.GetStringAsync(sb.ToString());
    }

    [McpServerTool(Name = "get_service_history"), Description("Returns the car details and full service history for a specific car by its ID.")]
    public static async Task<string> GetServiceHistory(
        HttpClient http,
        [Description("The car's database ID")] int carId)
        => await http.GetStringAsync($"api/cars/{carId}");

    [McpServerTool(Name = "inventory_summary"), Description("Returns a high-level summary: total cars, breakdown by status, average/min/max price.")]
    public static async Task<string> InventorySummary(HttpClient http)
    {
        var json = await http.GetStringAsync("api/cars");
        var cars = JsonNode.Parse(json)?.AsArray() ?? [];

        var prices = cars
            .Select(c => c?["price"]?.GetValue<double>())
            .Where(p => p is not null)
            .Select(p => p!.Value)
            .ToList();

        return JsonSerializer.Serialize(new
        {
            Total     = cars.Count,
            Available = cars.Count(c => c?["status"]?.GetValue<string>() == "Available"),
            Sold      = cars.Count(c => c?["status"]?.GetValue<string>() == "Sold"),
            Reserved  = cars.Count(c => c?["status"]?.GetValue<string>() == "Reserved"),
            AvgPrice  = prices.Count > 0 ? Math.Round(prices.Average(), 2) : (double?)null,
            MinPrice  = prices.Count > 0 ? prices.Min()                    : (double?)null,
            MaxPrice  = prices.Count > 0 ? prices.Max()                    : (double?)null,
        });
    }
}

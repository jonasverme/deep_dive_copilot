using CarInventory.Chat.Endpoints;
using CarInventory.Chat.Services;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddHttpClient("ollama", c => c.Timeout = TimeSpan.FromMinutes(10));
builder.Services.AddScoped<IOllamaService, OllamaService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// McpClientService is a singleton: it holds one stdio subprocess for the app lifetime.
builder.Services.AddSingleton<IMcpClientService>(sp =>
    McpClientService.CreateAsync(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<ILoggerFactory>())
    .GetAwaiter().GetResult());

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseDefaultFiles();
app.UseStaticFiles();

// ── Endpoints ─────────────────────────────────────────────────────────────────
app.MapChatEndpoints();

app.Run();

public partial class Program { }

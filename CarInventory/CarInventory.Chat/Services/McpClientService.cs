namespace CarInventory.Chat.Services;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using System.Text.Json;

public class McpClientService : IMcpClientService, IAsyncDisposable
{
    private readonly IMcpClient _client;

    private McpClientService(IMcpClient client) => _client = client;

    public static async Task<McpClientService> CreateAsync(
        IConfiguration config,
        ILoggerFactory loggerFactory,
        CancellationToken ct = default)
    {
        var dllPath = config["Mcp:DllPath"]
            ?? throw new InvalidOperationException("Mcp:DllPath is not configured.");
        var apiUrl = config["Mcp:ApiUrl"]
            ?? throw new InvalidOperationException("Mcp:ApiUrl is not configured.");

        var transport = new StdioClientTransport(
            new StdioClientTransportOptions
            {
                Command   = "dotnet",
                Arguments = [dllPath, apiUrl],
                Name      = "CarInventory.Mcp"
            },
            loggerFactory);

        var mcpClient = await McpClientFactory.CreateAsync(
            transport,
            new McpClientOptions
            {
                ClientInfo = new() { Name = "CarInventory.Chat", Version = "1.0.0" }
            },
            loggerFactory,
            ct);

        return new McpClientService(mcpClient);
    }

    public Task<IList<McpClientTool>> ListToolsAsync(CancellationToken ct = default)
        => _client.ListToolsAsync(cancellationToken: ct);

    public async Task<string> CallToolAsync(
        string                          toolName,
        Dictionary<string, JsonElement> arguments,
        CancellationToken               ct = default)
    {
        var args = arguments.ToDictionary(
            kv => kv.Key,
            kv => (object?)kv.Value);

        var result = await _client.CallToolAsync(toolName, args, cancellationToken: ct);

        var textContent = result.Content.FirstOrDefault(c => c.Type == "text");
        return textContent?.Text
               ?? JsonSerializer.Serialize(result.Content.Select(c => c.Text));
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is IAsyncDisposable d)
            await d.DisposeAsync();
    }
}

namespace CarInventory.Chat.Services;

using ModelContextProtocol.Client;
using System.Text.Json;

public interface IMcpClientService
{
    Task<IList<McpClientTool>> ListToolsAsync(CancellationToken ct = default);

    Task<string> CallToolAsync(
        string                          toolName,
        Dictionary<string, JsonElement> arguments,
        CancellationToken               ct = default);
}

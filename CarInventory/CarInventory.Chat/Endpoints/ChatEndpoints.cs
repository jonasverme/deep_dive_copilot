namespace CarInventory.Chat.Endpoints;

using CarInventory.Chat.Services;
using FluentValidation;
using System.Text.Json;

public static class ChatEndpoints
{
    private static readonly Dictionary<string, string> ToolLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["list_cars"]           = "📋 Listing cars…",
        ["search_cars"]         = "🔍 Searching cars…",
        ["get_service_history"] = "🔧 Fetching service history…",
        ["inventory_summary"]   = "📊 Getting inventory summary…",
    };

    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chat", async (
            ChatRequest             request,
            IOllamaService          ollama,
            IMcpClientService       mcp,
            IValidator<ChatRequest> validator,
            HttpContext             http,
            CancellationToken       ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            // ── Start SSE response before tool loop so status events can stream ─
            http.Response.Headers.ContentType  = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";
            http.Response.Headers.Connection   = "keep-alive";

            // ── 1. Fetch MCP tools and convert to Ollama format ───────────────
            var mcpTools    = await mcp.ListToolsAsync(ct);
            var ollamaTools = mcpTools.Select(t => new OllamaTool(
                Type:     "function",
                Function: new OllamaToolFunction(
                    Name:        t.Name,
                    Description: t.Description ?? string.Empty,
                    Parameters:  t.JsonSchema))).ToList();

            // ── 2. Build initial message history ──────────────────────────────
            var messages = new List<OllamaMessage>
            {
                new("system",
                    """
                    You are an expert car inventory assistant for a Belgian car dealership.
                    Use the provided tools to look up real inventory data. Never fabricate data.
                    Format prices in EUR. Format dates as DD/MM/YYYY.
                    """),
                new("user", request.Message)
            };

            // ── 3. Tool-calling loop with live status events ───────────────────
            const int MaxRounds = 5;
            for (int round = 0; round < MaxRounds; round++)
            {
                var response = await ollama.ChatAsync(messages, ollamaTools, ct);

                if (response.Message?.ToolCalls is not { Count: > 0 } toolCalls)
                {
                    if (response.Message is not null)
                        messages.Add(response.Message);
                    break;
                }

                messages.Add(response.Message!);

                foreach (var call in toolCalls)
                {
                    var label = ToolLabels.TryGetValue(call.Function.Name, out var l)
                        ? l : $"⚙️ Calling {call.Function.Name}…";

                    await WriteEventAsync(http, new { status = label }, ct);

                    var toolResult = await mcp.CallToolAsync(
                        call.Function.Name,
                        call.Function.Arguments,
                        ct);

                    messages.Add(new OllamaMessage(Role: "tool", Content: toolResult));
                }
            }

            // ── 4. Stream final answer as token events ────────────────────────
            await foreach (var token in ollama.StreamAsync(messages, ct))
            {
                await WriteEventAsync(http, new { token }, ct);
            }

            await http.Response.WriteAsync("data: [DONE]\n\n", ct);
            await http.Response.Body.FlushAsync(ct);

            return Results.Empty;
        });

        return app;
    }

    private static async Task WriteEventAsync(HttpContext http, object data, CancellationToken ct)
    {
        await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(data)}\n\n", ct);
        await http.Response.Body.FlushAsync(ct);
    }
}

public record ChatRequest(string Message);

public class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    public ChatRequestValidator()
    {
        RuleFor(r => r.Message)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(1000);
    }
}

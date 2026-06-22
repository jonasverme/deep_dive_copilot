namespace CarInventory.Chat.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

public record OllamaMessage(
    [property: JsonPropertyName("role")]       string Role,
    [property: JsonPropertyName("content")]    string? Content   = null,
    [property: JsonPropertyName("tool_calls")] List<OllamaToolCall>? ToolCalls = null);

public record OllamaToolCall(
    [property: JsonPropertyName("function")] OllamaToolCallFunction Function);

public record OllamaToolCallFunction(
    [property: JsonPropertyName("name")]      string Name,
    [property: JsonPropertyName("arguments")] Dictionary<string, JsonElement> Arguments);

public record OllamaTool(
    [property: JsonPropertyName("type")]     string Type,
    [property: JsonPropertyName("function")] OllamaToolFunction Function);

public record OllamaToolFunction(
    [property: JsonPropertyName("name")]        string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("parameters")]  JsonElement Parameters);

public record OllamaResponse(
    [property: JsonPropertyName("message")]           OllamaMessage? Message,
    [property: JsonPropertyName("done")]              bool Done,
    [property: JsonPropertyName("prompt_eval_count")] int? PromptEvalCount,
    [property: JsonPropertyName("eval_count")]        int? EvalCount);

public record StreamChunk(string? Token, int? PromptTokens, int? CompletionTokens);

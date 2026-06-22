namespace CarInventory.Chat.Services;

using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

public class OllamaService(IHttpClientFactory factory, IConfiguration config) : IOllamaService
{
    private readonly string _baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
    private readonly string _model   = config["Ollama:Model"]   ?? "qwen2.5";

    public async Task<OllamaResponse> ChatAsync(
        IList<OllamaMessage> messages,
        IList<OllamaTool>?   tools,
        CancellationToken    ct = default)
    {
        var client = factory.CreateClient("ollama");

        object body = tools is { Count: > 0 }
            ? new { model = _model, messages, tools, stream = false }
            : new { model = _model, messages,         stream = false };

        var response = await client.PostAsJsonAsync($"{_baseUrl}/api/chat", body, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Empty response from Ollama.");
    }

    public async IAsyncEnumerable<StreamChunk> StreamAsync(
        IList<OllamaMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var client = factory.CreateClient("ollama");
        var body   = new { model = _model, messages, stream = true };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
        {
            Content = JsonContent.Create(body)
        };
        using var response = await client.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            OllamaResponse? chunk;
            try { chunk = JsonSerializer.Deserialize<OllamaResponse>(line); }
            catch (JsonException) { continue; }

            if (chunk?.Message?.Content is { Length: > 0 } text)
                yield return new StreamChunk(text, null, null);

            if (chunk?.Done == true)
            {
                yield return new StreamChunk(null, chunk.PromptEvalCount, chunk.EvalCount);
                break;
            }
        }
    }
}

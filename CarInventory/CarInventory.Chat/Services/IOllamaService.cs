namespace CarInventory.Chat.Services;

public interface IOllamaService
{
    Task<OllamaResponse> ChatAsync(
        IList<OllamaMessage> messages,
        IList<OllamaTool>?   tools,
        CancellationToken    ct = default);

    IAsyncEnumerable<string> StreamAsync(
        IList<OllamaMessage> messages,
        CancellationToken    ct = default);
}

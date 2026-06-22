extern alias ChatAssembly;
using Program = ChatAssembly::Program;

using ChatAssembly::CarInventory.Chat.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CarInventory.Tests;

public class ChatEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChatEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(
        IOllamaService?    ollamaStub = null,
        IMcpClientService? mcpStub   = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var ollamaDesc = services.Single(d => d.ServiceType == typeof(IOllamaService));
                services.Remove(ollamaDesc);
                services.AddScoped<IOllamaService>(_ => ollamaStub ?? CreateDefaultOllamaStub());

                var mcpDesc = services.Single(d => d.ServiceType == typeof(IMcpClientService));
                services.Remove(mcpDesc);
                services.AddSingleton<IMcpClientService>(_ => mcpStub ?? CreateDefaultMcpStub());
            });
        }).CreateClient();
    }

    private static IOllamaService CreateDefaultOllamaStub()
    {
        var stub = Substitute.For<IOllamaService>();

        stub.ChatAsync(
                Arg.Any<IList<OllamaMessage>>(),
                Arg.Any<IList<OllamaTool>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new OllamaResponse(
                Message: new OllamaMessage("assistant", "Hello from Qwen!"),
                Done:    true));

        stub.StreamAsync(
                Arg.Any<IList<OllamaMessage>>(),
                Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableOf("Hello", " from", " Qwen!"));

        return stub;
    }

    private static IMcpClientService CreateDefaultMcpStub()
    {
        var stub = Substitute.For<IMcpClientService>();
        stub.ListToolsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<McpClientTool>>([]));
        return stub;
    }

    [Fact]
    public async Task POST_Chat_WithValidMessage_Returns200WithSSEStream()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/chat", new { message = "What cars are available?" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");
    }

    [Fact]
    public async Task POST_Chat_WithEmptyMessage_Returns400()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/chat", new { message = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Chat_WithTooLongMessage_Returns400()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/chat", new { message = new string('a', 1001) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Chat_StreamsTokensAsSSEEvents()
    {
        var ollamaStub = Substitute.For<IOllamaService>();
        ollamaStub.ChatAsync(
                Arg.Any<IList<OllamaMessage>>(),
                Arg.Any<IList<OllamaTool>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new OllamaResponse(
                Message: new OllamaMessage("assistant", "BMW 320i is available"),
                Done: true));

        ollamaStub.StreamAsync(
                Arg.Any<IList<OllamaMessage>>(),
                Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableOf("BMW", " 320i", " is available"));

        var client = CreateClient(ollamaStub);
        var response = await client.PostAsJsonAsync("/api/chat", new { message = "Any BMW available?" });
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("BMW");
        body.Should().Contain("[DONE]");
    }

    [Fact]
    public async Task POST_Chat_WhenModelCallsTool_InvokesMcpAndContinues()
    {
        var toolCallMessage = new OllamaMessage(
            Role: "assistant",
            ToolCalls:
            [
                new OllamaToolCall(new OllamaToolCallFunction(
                    "list_cars",
                    new Dictionary<string, JsonElement>()))
            ]);

        var ollamaStub = Substitute.For<IOllamaService>();
        ollamaStub.ChatAsync(
                Arg.Any<IList<OllamaMessage>>(),
                Arg.Any<IList<OllamaTool>?>(),
                Arg.Any<CancellationToken>())
            .Returns(
                new OllamaResponse(toolCallMessage, Done: false),
                new OllamaResponse(new OllamaMessage("assistant", "Here are the cars"), Done: true));

        ollamaStub.StreamAsync(
                Arg.Any<IList<OllamaMessage>>(),
                Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableOf("Here are the cars"));

        var mcpStub = Substitute.For<IMcpClientService>();
        mcpStub.ListToolsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<McpClientTool>>([]));
        mcpStub.CallToolAsync(
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, JsonElement>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("[{\"Make\":\"BMW\"}]"));

        var client = CreateClient(ollamaStub, mcpStub);
        var response = await client.PostAsJsonAsync("/api/chat", new { message = "List all cars" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await mcpStub.Received(1).CallToolAsync(
            "list_cars",
            Arg.Any<Dictionary<string, JsonElement>>(),
            Arg.Any<CancellationToken>());
    }

    private static async IAsyncEnumerable<string> AsyncEnumerableOf(params string[] tokens)
    {
        foreach (var t in tokens)
        {
            await Task.Yield();
            yield return t;
        }
    }
}

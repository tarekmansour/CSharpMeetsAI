using Microsoft.Extensions.AI;

using OllamaSharp.Models.Chat;

namespace CSharpMeetsAI.Api.Services;

public class ChatService(IChatClient chatClient, ILogger<ChatService> logger)
{
    private TimeSpan DefaultStreamItemTimeout = TimeSpan.FromMinutes(1);
    private const string ContentType = "text/plain; charset=utf-8";
    private readonly IChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    private readonly ILogger<ChatService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<IResult> Chat(string prompt, HttpResponse response, CancellationToken cancellationToken = default)
    {
        response.ContentType = ContentType;

        try
        {
            _logger.LogInformation("Streaming chat response started. Prompt: {Prompt}", prompt);
            await StreamResponseAsync(prompt, response, cancellationToken);
            _logger.LogInformation("Streaming chat response completed successfully.");

            return Results.Empty;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Chat stream canceled by client.");
            return Results.StatusCode(499); // Client Closed Request
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error while processing chat stream.");
            return Results.Problem(
                title: "Chat Processing Error",
                detail: "An error occurred while processing the chat stream.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private async Task StreamResponseAsync(string prompt, HttpResponse response, CancellationToken cancellationToken)
    {
        await foreach (var message in _chatClient.GetStreamingResponseAsync(prompt).WithCancellation(cancellationToken))
        {
            if (!string.IsNullOrEmpty(message.Text))
            {
                await response.WriteAsync(message.Text, cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
            }
        }
    }


    public async Task<string> Stream(string prompt, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Streaming chat response started. Prompt: {Prompt}", prompt);
        var allChunks = new List<ChatResponseUpdate>();

        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        tokenSource.CancelAfter(DefaultStreamItemTimeout);

        await foreach (var update in chatClient.GetStreamingResponseAsync(chatMessage: prompt, cancellationToken: cancellationToken))
        {
            // Extend the cancellation token's timeout for each update.
            tokenSource.CancelAfter(DefaultStreamItemTimeout);

            if (update.Text is not null)
            {
                allChunks.Add(update);
            }
        }

        _logger.LogInformation("Streaming chat response completed successfully.");

        if (allChunks.Count > 0)
        {
            var fullMessage = allChunks.ToChatResponse().Text;
            return fullMessage;
        }

        return string.Empty;
    }

}
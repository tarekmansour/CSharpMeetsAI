using CSharpMeetsAI.Api.Requests;

using Microsoft.Extensions.AI;

namespace CSharpMeetsAI.Api.Services;

public class ChatService(IChatClient chatClient, ILogger<ChatService> logger)
{
    private TimeSpan DefaultStreamItemTimeout = TimeSpan.FromMinutes(1);
    private const string ContentType = "text/plain; charset=utf-8";
    private readonly IChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    private readonly ILogger<ChatService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<IResult> ChatAsync(string prompt, HttpResponse response, CancellationToken cancellationToken = default)
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
        await foreach (var message in _chatClient.GetStreamingResponseAsync(chatMessage: prompt).WithCancellation(cancellationToken))
        {
            if (!string.IsNullOrEmpty(message.Text))
            {
                await response.WriteAsync(message.Text, cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
            }
        }
    }

    public async Task<string> StreamWithChunksAsync(string prompt, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Streaming chat response started. Prompt: {Prompt}", prompt);
        var allChunks = new List<ChatResponseUpdate>();
        try
        {
            //creates a linked cancellation token that:
                //Combines the original cancellationToken (from the HTTP request)
                //Adds a timeout that resets every time you receive a new chunk
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
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Chat stream canceled by client.");

            if (allChunks.Count > 0)
            {
                var fullMessage = allChunks.ToChatResponse().Text;
                return fullMessage;
            }
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing chat stream.");
            throw;
        }
    }

    public async Task<IResult> StreamResponseToClientAsync(AiChatRequest request, HttpResponse response, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        tokenSource.CancelAfter(DefaultStreamItemTimeout);

        try
        {
            _logger.LogInformation("Streaming started for model {Model}. Prompt: {Prompt}", request.Model, request.Prompt);

            await foreach (var update in _chatClient.GetStreamingResponseAsync(chatMessage: request.Prompt, cancellationToken: tokenSource.Token))
            {
                tokenSource.CancelAfter(DefaultStreamItemTimeout); // rolling timeout

                if (!string.IsNullOrEmpty(update.Text))
                {
                    await response.WriteAsync(update.Text, cancellationToken);
                    await response.Body.FlushAsync(cancellationToken);
                }
            }

            _logger.LogInformation("Streaming completed successfully.");
            return Results.Empty;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Streaming canceled.");
            return Results.StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streaming error.");
            return Results.Problem("An error occurred during streaming.");
        }
    }
}
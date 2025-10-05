using Microsoft.Extensions.AI;

namespace CSharpMeetsAI.Api.Services;

public class ChatService(IChatClient chatClient, ILogger<ChatService> logger)
{
    public async Task<IResult> Chat(string prompt, HttpResponse response)
    {
        response.ContentType = "text/plain; charset=utf-8";

        try
        {
            logger.LogInformation("Streaming chat response started. Prompt: {Prompt}", prompt);

            await foreach (var message in chatClient.GetStreamingResponseAsync(prompt))
            {
                if (!string.IsNullOrEmpty(message.Text))
                {
                    await response.WriteAsync(message.Text);
                    await response.Body.FlushAsync();
                }
            }

            logger.LogInformation("Streaming chat response completed successfully.");
            return Results.Empty;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Chat stream canceled by client.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while processing chat stream.");
            throw;
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

using OllamaSharp.Models.Chat;

namespace CSharpMeetsAI.Api;

public static class Endpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        app.MapPost("v1/api/chat", async (
            [FromBody] string prompt,
            [FromServices] IChatClient chatClient) =>
        {
            var response = await chatClient.GetResponseAsync(prompt);

            return Results.Ok(response);
        });

        app.MapPost("v2/api/chat", async (
            [FromBody] string prompt,
            [FromServices] IChatClient chatClient,
            [FromServices] ILoggerFactory loggerFactory,
            HttpResponse response) =>
        {
            var logger = loggerFactory.CreateLogger("ChatEndpoint");

            if (string.IsNullOrWhiteSpace(prompt))
            {
                logger.LogWarning("Empty prompt received at /v2/api/chat");
                return Results.BadRequest("Prompt cannot be empty.");
            }

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
        });
    }
}
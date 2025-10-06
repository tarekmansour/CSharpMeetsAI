using CSharpMeetsAI.Api.Requests;
using CSharpMeetsAI.Api.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace CSharpMeetsAI.Api;

public static class Endpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/chat");

        //single-shot response: no streaming
        group.MapPost("/v1", async (
            [FromBody] string prompt,
            [FromServices] IChatClient chatClient) =>
        {
            var response = await chatClient.GetResponseAsync(prompt);
            return Results.Ok(response);
        });

        //Streaming
        group.MapPost("/v2", async (
            [FromBody] string prompt,
            ChatService chatService,
            HttpResponse response,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return Results.BadRequest("Prompt cannot be empty.");
            }

            return await chatService.ChatAsync(prompt, response, cancellationToken);
        });

        //Streaming with Timeout Control
        group.MapPost("/v3", async (
            [FromBody] string prompt,
            ChatService chatService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return Results.BadRequest("Prompt cannot be empty.");
            }

            var response = await chatService.StreamWithChunksAsync(prompt, cancellationToken);
            return Results.Ok(response);
        });

        //Streaming with full request options
        group.MapPost("/v4", async (
            [FromBody] AiChatRequest request,
            ChatService chatService,
            HttpResponse response,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return Results.BadRequest("Prompt cannot be empty.");
            }

            response.ContentType = "text/plain; charset=utf-8";

            return await chatService.StreamResponseToClientAsync(request, response, cancellationToken);
        });
    }
}
using System;

using CSharpMeetsAI.Api.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace CSharpMeetsAI.Api;

public static class Endpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/");

        group.MapPost("v1/chat", async (
            [FromBody] string prompt,
            [FromServices] IChatClient chatClient) =>
        {
            var response = await chatClient.GetResponseAsync(prompt);
            return Results.Ok(response);
        });

        group.MapPost("v2/chat", async (
            [FromBody] string prompt,
            ChatService chatService,
            HttpResponse response,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return Results.BadRequest("Prompt cannot be empty.");
            }

            return await chatService.Chat(prompt, response, cancellationToken);
        });

        group.MapPost("v3/chat", async (
            [FromBody] string prompt,
            ChatService chatService,
            CancellationToken cancellationToken) =>
        {
            var response = await chatService.Stream(prompt, cancellationToken);

            return Results.Ok(response);
        });
    }
}
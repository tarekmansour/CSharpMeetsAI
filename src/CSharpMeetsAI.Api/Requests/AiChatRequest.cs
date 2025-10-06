namespace CSharpMeetsAI.Api.Requests;

public class AiChatRequest
{
    public required string Prompt { get; init; }
    public string? SystemMessage { get; init; }
    public string? Model { get; init; } = "phi3:mini"; // default
}


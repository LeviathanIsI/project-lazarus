namespace Lazarus.Shared.Contracts.Chat;

public sealed class ChatRequest
{
    public required string Model { get; init; }
    public required string[] Messages { get; init; }
    public bool Stream { get; init; } = true;
}


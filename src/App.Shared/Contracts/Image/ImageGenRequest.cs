namespace Lazarus.Shared.Contracts.Image;

public sealed class ImageGenRequest
{
    public required string Prompt { get; init; }
    public string? NegativePrompt { get; init; }
    public int Width { get; init; } = 768;
    public int Height { get; init; } = 768;
    public int Steps { get; init; } = 28;
    public float CfgScale { get; init; } = 6.5f;

    // Which image runner to use (NOT chat)
    public required string RunnerId { get; init; }
}


// src/App.Shared/OpenAIContracts.cs
using System.Text.Json.Serialization;

namespace Lazarus.Shared.OpenAI;

/// <summary>
/// Minimal OpenAI-compatible chat request.
/// </summary>
public sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")] public string? Model { get; set; }

    // Required by OpenAI schema
    [JsonPropertyName("messages")] public List<ChatMessage> Messages { get; set; } = new();

    // Optional knobs (not strictly needed but nice to have)
    [JsonPropertyName("temperature")] public double? Temperature { get; set; }
    [JsonPropertyName("max_tokens")] public int? MaxTokens { get; set; }
    [JsonPropertyName("stream")] public bool? Stream { get; set; }
}

public sealed class ChatMessage
{
    [JsonPropertyName("role")] public string Role { get; set; } = "user";   // "system" | "user" | "assistant"
    [JsonPropertyName("content")] public string Content { get; set; } = "";
}

/// <summary>
/// Minimal OpenAI-compatible chat response.
/// </summary>
public sealed class ChatCompletionResponse
{
    [JsonPropertyName("id")] public string Id { get; set; } = $"chatcmpl_{Guid.NewGuid():N}";
    [JsonPropertyName("object")] public string Object { get; set; } = "chat.completion";
    [JsonPropertyName("created")] public long Created { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    [JsonPropertyName("model")] public string Model { get; set; } = "local-dev";

    [JsonPropertyName("choices")] public List<Choice> Choices { get; set; } = new()
    {
        new Choice
        {
            Index = 0,
            Message = new ChatMessage { Role = "assistant", Content = "pong" },
            FinishReason = "stop"
        }
    };

    [JsonPropertyName("usage")] public Usage Usage { get; set; } = new();
}

public sealed class Choice
{
    [JsonPropertyName("index")] public int Index { get; set; }

    // OpenAI returns "message" with role/content
    [JsonPropertyName("message")] public ChatMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")] public string FinishReason { get; set; } = "stop";
}

public sealed class Usage
{
    [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }
    [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; set; }
    [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
}

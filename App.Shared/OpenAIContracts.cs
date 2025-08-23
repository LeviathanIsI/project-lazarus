// src/App.Shared/OpenAIContracts.cs
using System.Text.Json.Serialization;

namespace Lazarus.Shared.OpenAI;

/// <summary>
/// Minimal OpenAI-compatible chat request.
/// </summary>
public sealed class ChatMessage
{
    [JsonPropertyName("role")] public string Role { get; set; } = "user";   // "system" | "user" | "assistant"
    [JsonPropertyName("content")] public string Content { get; set; } = "";
}

/// <summary>
/// Minimal OpenAI-compatible chat response.
/// </summary>
public sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")] public string? Model { get; set; }
    [JsonPropertyName("messages")] public List<ChatMessage> Messages { get; set; } = new();

    // Core parameters
    [JsonPropertyName("temperature")] public double? Temperature { get; set; }
    [JsonPropertyName("max_tokens")] public int? MaxTokens { get; set; }
    [JsonPropertyName("stream")] public bool? Stream { get; set; }

    // Advanced sampling parameters - the full arsenal
    [JsonPropertyName("top_p")] public double? TopP { get; set; }
    [JsonPropertyName("top_k")] public int? TopK { get; set; }
    [JsonPropertyName("min_p")] public double? MinP { get; set; }
    [JsonPropertyName("typical_p")] public double? TypicalP { get; set; }

    // Repetition control
    [JsonPropertyName("repetition_penalty")] public double? RepetitionPenalty { get; set; }
    [JsonPropertyName("frequency_penalty")] public double? FrequencyPenalty { get; set; }
    [JsonPropertyName("presence_penalty")] public double? PresencePenalty { get; set; }

    // Advanced controls
    [JsonPropertyName("seed")] public int? Seed { get; set; }
    [JsonPropertyName("mirostat")] public int? MirostatMode { get; set; }
    [JsonPropertyName("mirostat_tau")] public double? MirostatTau { get; set; }
    [JsonPropertyName("mirostat_eta")] public double? MirostatEta { get; set; }

    // Additional controls
    [JsonPropertyName("tfs_z")] public double? TfsZ { get; set; }
    [JsonPropertyName("eta_cutoff")] public double? EtaCutoff { get; set; }
    [JsonPropertyName("epsilon_cutoff")] public double? EpsilonCutoff { get; set; }
}

public sealed class ChatCompletionResponse
{
    [JsonPropertyName("id")] public string Id { get; set; } = $"chatcmpl_{Guid.NewGuid():N}";
    [JsonPropertyName("object")] public string Object { get; set; } = "chat.completion";
    [JsonPropertyName("created")] public long Created { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    [JsonPropertyName("model")] public string Model { get; set; } = "local-dev";

    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; } = new()
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

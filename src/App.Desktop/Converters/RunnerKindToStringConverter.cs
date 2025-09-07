using System;
using System.Globalization;
using System.Windows.Data;
using Lazarus.Shared;

namespace Lazarus.Desktop.Converters;

public sealed class RunnerKindToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RunnerKind kind)
        {
            return kind switch
            {
                RunnerKind.LlamaCpp => "llama.cpp",
                RunnerKind.Vllm => "vLLM",
                RunnerKind.ExLlamaV2 => "exllamav2",
                _ => "unknown"
            };
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}


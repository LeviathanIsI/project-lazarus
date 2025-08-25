using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Lazarus.Shared.Models;
using Lazarus.Orchestrator.Runners;

namespace Lazarus.Orchestrator.Services;

/// <summary>
/// Interrogates loaded models to discover their actual parameter capabilities
/// No more pretending all models are identical!
/// </summary>
public static class ModelIntrospectionService
{
    private static readonly ILogger Logger = LoggerFactory.Create(builder =>
        builder.AddConsole().SetMinimumLevel(LogLevel.Debug)
    ).CreateLogger("ModelIntrospection");

    private static readonly Dictionary<string, ModelCapabilities> _capabilityCache = new();
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Deep introspection - discover what this model can actually do
    /// </summary>
    public static async Task<ModelCapabilities> IntrospectModelAsync(IChatRunner runner, string modelPath)
    {
        Logger.LogInformation($"🔍 Beginning deep introspection of model: {modelPath}");
        
        var modelName = ExtractModelName(modelPath);
        
        // Check cache first
        if (_capabilityCache.TryGetValue(modelName, out var cached) && 
            DateTime.UtcNow - cached.DetectionTimestamp < TimeSpan.FromHours(1))
        {
            Logger.LogDebug($"Using cached capabilities for {modelName}");
            return cached;
        }

        var capabilities = new ModelCapabilities
        {
            ModelName = modelName,
            ModelFamily = DetectModelFamily(modelName),
            DetectionTimestamp = DateTime.UtcNow
        };

        try
        {
            // Step 1: Extract metadata from model file/path
            await ExtractModelMetadataAsync(capabilities, modelPath);
            
            // Step 2: Query runner for supported parameters
            await QueryRunnerCapabilitiesAsync(capabilities, runner);
            
            // Step 3: Apply model family-specific knowledge
            ApplyModelFamilyProfile(capabilities);
            
            // Step 4: Detect parameter interdependencies 
            DetectParameterDependencies(capabilities);
            
            // Step 5: Generate model-optimized defaults
            GenerateModelDefaults(capabilities);
            
            // Step 6: Test parameter modification permissions
            await TestParameterModifiabilityAsync(capabilities, runner);
            
            Logger.LogInformation($"✅ Model introspection complete - {capabilities.AvailableParameters.Count} parameters discovered");
            
            _capabilityCache[modelName] = capabilities;
            return capabilities;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"❌ Model introspection failed for {modelName}");
            
            // Return basic capabilities as fallback
            return CreateFallbackCapabilities(modelName);
        }
    }

    /// <summary>
    /// Extract metadata from model name and path
    /// </summary>
    private static async Task ExtractModelMetadataAsync(ModelCapabilities capabilities, string modelPath)
    {
        Logger.LogDebug($"Extracting metadata from: {modelPath}");
        
        var modelName = capabilities.ModelName.ToLowerInvariant();
        
        // Parameter count detection
        if (Regex.IsMatch(modelName, @"(\d+)b"))
        {
            var match = Regex.Match(modelName, @"(\d+)b");
            if (int.TryParse(match.Groups[1].Value, out var paramCount))
            {
                capabilities.ParameterCount = (long)paramCount * 1_000_000_000L;
                capabilities.Class = paramCount switch
                {
                    < 1 => ModelClass.Basic,
                    < 7 => ModelClass.Standard, 
                    < 30 => ModelClass.Advanced,
                    _ => ModelClass.Experimental
                };
            }
        }
        
        // Context length detection
        if (modelName.Contains("32k")) capabilities.ContextLength = 32768;
        else if (modelName.Contains("16k")) capabilities.ContextLength = 16384;
        else if (modelName.Contains("8k")) capabilities.ContextLength = 8192;
        else if (modelName.Contains("4k")) capabilities.ContextLength = 4096;
        else capabilities.ContextLength = 2048; // Conservative default
        
        // Quantization detection
        if (modelName.Contains("q4_k_m")) capabilities.Quantization = "Q4_K_M";
        else if (modelName.Contains("q5_k_m")) capabilities.Quantization = "Q5_K_M";
        else if (modelName.Contains("q8_0")) capabilities.Quantization = "Q8_0";
        else if (modelName.Contains("f16")) capabilities.Quantization = "F16";
        else if (modelName.Contains("f32")) capabilities.Quantization = "F32";
        
        Logger.LogDebug($"Model metadata: {capabilities.ParameterCount / 1_000_000_000.0:F1}B params, {capabilities.ContextLength} context, {capabilities.Quantization} quant");
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Query the runner to see what parameters it actually supports
    /// </summary>
    private static async Task QueryRunnerCapabilitiesAsync(ModelCapabilities capabilities, IChatRunner runner)
    {
        Logger.LogDebug("Querying runner for parameter support...");
        
        try
        {
            // Try a test request with various parameters to see what's accepted
            var testRequest = new Lazarus.Shared.OpenAI.ChatCompletionRequest
            {
                Model = capabilities.ModelName,
                Messages = new() { new() { Role = "user", Content = "Hi" } },
                MaxTokens = 1,
                
                // Test core parameters
                Temperature = 0.7f,
                TopP = 0.9f,
                TopK = 40,
                FrequencyPenalty = 0.1f,
                PresencePenalty = 0.1f,
                
                // Test advanced parameters
                MinP = 0.05f,
                TypicalP = 0.95f,
                RepetitionPenalty = 1.1f,
                
                // Test experimental parameters  
                MirostatMode = 0,
                MirostatTau = 5.0f,
                MirostatEta = 0.1f,
                
                TfsZ = 1.0f,
                Seed = 12345
            };
            
            var response = await runner.ChatAsync(testRequest);
            
            if (response != null)
            {
                Logger.LogDebug("✅ Test request successful - model accepts parameter modifications");
                
                // Add standard parameters that worked
                AddParameterCapability(capabilities, "Temperature", ParameterType.Float, 0.0f, 2.0f, 0.7f);
                AddParameterCapability(capabilities, "TopP", ParameterType.Float, 0.0f, 1.0f, 0.9f);
                AddParameterCapability(capabilities, "TopK", ParameterType.Integer, 1, 200, 40);
                AddParameterCapability(capabilities, "MaxTokens", ParameterType.Integer, 1, capabilities.ContextLength, 1024);
                AddParameterCapability(capabilities, "FrequencyPenalty", ParameterType.Float, -2.0f, 2.0f, 0.0f);
                AddParameterCapability(capabilities, "PresencePenalty", ParameterType.Float, -2.0f, 2.0f, 0.0f);
                AddParameterCapability(capabilities, "Seed", ParameterType.Integer, -1, int.MaxValue, -1);
                
                // Test advanced parameters individually
                await TestAdvancedParameters(capabilities, runner);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Parameter testing failed - using basic parameter set");
            
            // Add minimal safe parameters
            AddParameterCapability(capabilities, "Temperature", ParameterType.Float, 0.1f, 1.5f, 0.7f);
            AddParameterCapability(capabilities, "MaxTokens", ParameterType.Integer, 1, 2048, 1024);
        }
    }

    /// <summary>
    /// Test advanced parameters individually to see what's supported
    /// </summary>
    private static async Task TestAdvancedParameters(ModelCapabilities capabilities, IChatRunner runner)
    {
        var advancedTests = new Dictionary<string, object>
        {
            ["MinP"] = 0.05f,
            ["TypicalP"] = 0.95f,
            ["RepetitionPenalty"] = 1.1f,
            ["TfsZ"] = 0.95f,
            ["MirostatMode"] = 1,
            ["MirostatTau"] = 5.0f,
            ["MirostatEta"] = 0.1f
        };

        foreach (var (paramName, testValue) in advancedTests)
        {
            try
            {
                var testRequest = new Lazarus.Shared.OpenAI.ChatCompletionRequest
                {
                    Model = capabilities.ModelName,
                    Messages = new() { new() { Role = "user", Content = "Test" } },
                    MaxTokens = 1
                };

                // Set the specific parameter
                var property = testRequest.GetType().GetProperty(paramName);
                property?.SetValue(testRequest, testValue);

                var response = await runner.ChatAsync(testRequest);
                
                if (response != null)
                {
                    Logger.LogDebug($"✅ Parameter {paramName} supported");
                    
                    // Add parameter based on type and known ranges
                    AddAdvancedParameter(capabilities, paramName, testValue);
                }
            }
            catch
            {
                Logger.LogDebug($"❌ Parameter {paramName} not supported or causes errors");
                capabilities.UnsupportedParameters.Add(paramName);
            }
        }
    }

    /// <summary>
    /// Apply model family-specific knowledge and optimizations
    /// </summary>
    private static void ApplyModelFamilyProfile(ModelCapabilities capabilities)
    {
        if (ModelFamilyProfiles.Profiles.TryGetValue(capabilities.ModelFamily, out var profile))
        {
            Logger.LogDebug($"Applying {capabilities.ModelFamily} family profile");
            
            // Update defaults based on family knowledge
            if (capabilities.AvailableParameters.TryGetValue("Temperature", out var tempParam))
            {
                tempParam.DefaultValue = profile.DefaultTemperature;
                capabilities.RecommendedDefaults["Temperature"] = profile.DefaultTemperature;
            }
            
            if (capabilities.AvailableParameters.TryGetValue("TopP", out var topPParam))
            {
                topPParam.DefaultValue = profile.DefaultTopP;
                capabilities.RecommendedDefaults["TopP"] = profile.DefaultTopP;
            }
            
            // Mark problematic parameters
            foreach (var problematic in profile.ProblematicParameters)
            {
                if (capabilities.AvailableParameters.TryGetValue(problematic, out var param))
                {
                    param.IsRecommended = false;
                    param.ModelSpecificDescription = $"Not recommended for {capabilities.ModelFamily} models";
                }
            }
            
            // Mark excellent parameters
            foreach (var excellent in profile.ExcellentParameters)
            {
                if (capabilities.AvailableParameters.TryGetValue(excellent, out var param))
                {
                    param.IsRecommended = true;
                    param.ModelSpecificDescription = $"Works excellently with {capabilities.ModelFamily} models";
                }
            }
            
            // Add family-specific warnings
            capabilities.ModelWarnings.AddRange(profile.SpecialBehaviors);
        }
    }

    /// <summary>
    /// Detect parameter interdependencies specific to this model
    /// </summary>
    private static void DetectParameterDependencies(ModelCapabilities capabilities)
    {
        // Mirostat disables Temperature
        if (capabilities.AvailableParameters.ContainsKey("MirostatMode") && 
            capabilities.AvailableParameters.ContainsKey("Temperature"))
        {
            capabilities.Dependencies.Add(new ParameterDependency
            {
                TriggerParameter = "MirostatMode",
                Condition = ComparisonType.GreaterThan,
                TriggerValue = 0,
                AffectedParameter = "Temperature",
                Action = DependencyAction.Hide,
                Warning = "Temperature is ignored when Mirostat is enabled"
            });
        }
        
        // High temperature may cause instability with certain quantizations
        if (capabilities.Quantization.StartsWith("Q4") && 
            capabilities.AvailableParameters.ContainsKey("Temperature"))
        {
            capabilities.Dependencies.Add(new ParameterDependency
            {
                TriggerParameter = "Temperature",
                Condition = ComparisonType.GreaterThan,
                TriggerValue = 1.2f,
                AffectedParameter = "Temperature",
                Action = DependencyAction.ShowWarning,
                Warning = "High temperature may cause instability with Q4 quantization"
            });
        }
        
        // Small models may not benefit from complex sampling
        if (capabilities.ParameterCount < 7_000_000_000L) // < 7B
        {
            foreach (var experimentalParam in new[] { "TfsZ", "EtaCutoff", "EpsilonCutoff", "DryMultiplier" })
            {
                if (capabilities.AvailableParameters.ContainsKey(experimentalParam))
                {
                    capabilities.AvailableParameters[experimentalParam].IsRecommended = false;
                    capabilities.AvailableParameters[experimentalParam].ModelSpecificDescription = "May not be effective on smaller models";
                }
            }
        }
    }

    /// <summary>
    /// Generate model-optimized defaults instead of hardcoded values
    /// </summary>
    private static void GenerateModelDefaults(ModelCapabilities capabilities)
    {
        // Adjust defaults based on model size
        if (capabilities.ParameterCount > 30_000_000_000L) // > 30B
        {
            capabilities.RecommendedDefaults["Temperature"] = 0.6f; // Large models can be more conservative
            capabilities.RecommendedDefaults["TopP"] = 0.85f;
        }
        else if (capabilities.ParameterCount < 3_000_000_000L) // < 3B
        {
            capabilities.RecommendedDefaults["Temperature"] = 0.8f; // Small models need more creativity
            capabilities.RecommendedDefaults["TopP"] = 0.95f;
        }
        
        // Adjust for quantization impact
        if (capabilities.Quantization.StartsWith("Q4"))
        {
            // Lower precision needs slightly higher temperature for diversity
            if (capabilities.RecommendedDefaults.TryGetValue("Temperature", out var temp) && temp is float tempVal)
            {
                capabilities.RecommendedDefaults["Temperature"] = Math.Min(tempVal + 0.1f, 1.2f);
            }
        }
        
        // Context-aware max tokens
        var suggestedMaxTokens = Math.Min(capabilities.ContextLength / 4, 2048); // Use 25% of context
        capabilities.RecommendedDefaults["MaxTokens"] = suggestedMaxTokens;
        
        Logger.LogDebug($"Generated {capabilities.RecommendedDefaults.Count} model-optimized defaults");
    }

    /// <summary>
    /// Test if parameters can actually be modified at runtime
    /// </summary>
    private static async Task TestParameterModifiabilityAsync(ModelCapabilities capabilities, IChatRunner runner)
    {
        Logger.LogDebug("Testing parameter modification permissions...");
        
        // Test if temperature changes actually affect output
        try
        {
            var lowTempRequest = CreateTestRequest(capabilities.ModelName, temperature: 0.1f);
            var highTempRequest = CreateTestRequest(capabilities.ModelName, temperature: 1.5f);
            
            var lowResponse = await runner.ChatAsync(lowTempRequest);
            var highResponse = await runner.ChatAsync(highTempRequest);
            
            // If responses are identical, temperature might be locked
            if (lowResponse?.Choices?[0]?.Message?.Content == highResponse?.Choices?[0]?.Message?.Content)
            {
                Logger.LogWarning("Temperature appears to be locked - responses identical");
                if (capabilities.AvailableParameters.TryGetValue("Temperature", out var tempParam))
                {
                    tempParam.IsModifiable = false;
                    capabilities.UnsupportedParameters.Add("Temperature");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug($"Parameter modifiability test failed: {ex.Message}");
        }
    }

    #region Helper Methods

    private static string ExtractModelName(string modelPath)
    {
        return Path.GetFileNameWithoutExtension(modelPath);
    }

    private static string DetectModelFamily(string modelName)
    {
        var name = modelName.ToLowerInvariant();
        
        if (name.Contains("qwen")) return "qwen";
        if (name.Contains("llama")) return "llama";
        if (name.Contains("mistral")) return "mistral";
        if (name.Contains("gemma")) return "gemma";
        if (name.Contains("phi")) return "phi";
        if (name.Contains("codellama")) return "llama"; // CodeLlama is LLaMA family
        
        return "unknown";
    }

    private static void AddParameterCapability(ModelCapabilities capabilities, string name, ParameterType type, object minValue, object maxValue, object defaultValue)
    {
        capabilities.AvailableParameters[name] = new ParameterCapability
        {
            Name = name,
            Type = type,
            MinValue = minValue,
            MaxValue = maxValue,
            DefaultValue = defaultValue,
            IsModifiable = true,
            IsRecommended = true
        };
    }

    private static void AddAdvancedParameter(ModelCapabilities capabilities, string paramName, object testValue)
    {
        var param = paramName switch
        {
            "MinP" => new ParameterCapability { Name = "MinP", Type = ParameterType.Float, MinValue = 0.0f, MaxValue = 1.0f, DefaultValue = 0.05f },
            "TypicalP" => new ParameterCapability { Name = "TypicalP", Type = ParameterType.Float, MinValue = 0.0f, MaxValue = 1.0f, DefaultValue = 0.95f },
            "RepetitionPenalty" => new ParameterCapability { Name = "RepetitionPenalty", Type = ParameterType.Float, MinValue = 0.5f, MaxValue = 2.0f, DefaultValue = 1.1f },
            "TfsZ" => new ParameterCapability { Name = "TfsZ", Type = ParameterType.Float, MinValue = 0.0f, MaxValue = 1.0f, DefaultValue = 1.0f, IsExperimental = true },
            "MirostatMode" => new ParameterCapability { Name = "MirostatMode", Type = ParameterType.Integer, MinValue = 0, MaxValue = 2, DefaultValue = 0, AllowedValues = new List<object> { 0, 1, 2 } },
            "MirostatTau" => new ParameterCapability { Name = "MirostatTau", Type = ParameterType.Float, MinValue = 1.0f, MaxValue = 10.0f, DefaultValue = 5.0f },
            "MirostatEta" => new ParameterCapability { Name = "MirostatEta", Type = ParameterType.Float, MinValue = 0.01f, MaxValue = 1.0f, DefaultValue = 0.1f },
            _ => null
        };

        if (param != null)
        {
            param.IsModifiable = true;
            param.IsRecommended = true;
            capabilities.AvailableParameters[paramName] = param;
        }
    }

    private static Lazarus.Shared.OpenAI.ChatCompletionRequest CreateTestRequest(string modelName, float temperature = 0.7f)
    {
        return new Lazarus.Shared.OpenAI.ChatCompletionRequest
        {
            Model = modelName,
            Messages = new() { new() { Role = "user", Content = "Say 'test' and nothing else." } },
            MaxTokens = 10,
            Temperature = temperature
        };
    }

    private static ModelCapabilities CreateFallbackCapabilities(string modelName)
    {
        Logger.LogWarning($"Creating fallback capabilities for {modelName}");
        
        var capabilities = new ModelCapabilities
        {
            ModelName = modelName,
            ModelFamily = "unknown",
            Class = ModelClass.Standard,
            DetectionTimestamp = DateTime.UtcNow
        };
        
        // Add only the most basic parameters
        AddParameterCapability(capabilities, "Temperature", ParameterType.Float, 0.1f, 1.5f, 0.7f);
        AddParameterCapability(capabilities, "MaxTokens", ParameterType.Integer, 1, 2048, 1024);
        
        return capabilities;
    }

    /// <summary>
    /// Update model capabilities to reflect applied LoRAs
    /// </summary>
    public static ModelCapabilities UpdateCapabilitiesWithLoRAs(ModelCapabilities baseCapabilities, List<AppliedLoRAInfo> appliedLoRAs)
    {
        Logger.LogInformation($"Updating model capabilities with {appliedLoRAs.Count} applied LoRAs");
        
        // Create a copy to avoid modifying the original
        var updatedCapabilities = new ModelCapabilities
        {
            ModelName = baseCapabilities.ModelName,
            ModelFamily = baseCapabilities.ModelFamily,
            Architecture = baseCapabilities.Architecture,
            Class = baseCapabilities.Class,
            ParameterCount = baseCapabilities.ParameterCount,
            ContextLength = baseCapabilities.ContextLength,
            Quantization = baseCapabilities.Quantization,
            AvailableParameters = new Dictionary<string, ParameterCapability>(baseCapabilities.AvailableParameters),
            Dependencies = new List<ParameterDependency>(baseCapabilities.Dependencies),
            RecommendedDefaults = new Dictionary<string, object>(baseCapabilities.RecommendedDefaults),
            UnsupportedParameters = new HashSet<string>(baseCapabilities.UnsupportedParameters),
            ModelWarnings = new List<string>(baseCapabilities.ModelWarnings),
            DetectionTimestamp = DateTime.UtcNow,
            AppliedLoRAs = new List<AppliedLoRAInfo>(appliedLoRAs),
            LoRAModifications = new Dictionary<string, LoRAParameterModification>()
        };
        
        // Apply LoRA-specific modifications
        foreach (var lora in appliedLoRAs.Where(l => l.IsEnabled))
        {
            ApplyLoRAModifications(updatedCapabilities, lora);
        }
        
        // Add LoRA-specific warnings and information
        if (updatedCapabilities.HasActiveLoRAs)
        {
            updatedCapabilities.ModelWarnings.Add($"Model has {updatedCapabilities.AppliedLoRAs.Count(l => l.IsEnabled)} active LoRA(s) with total weight {updatedCapabilities.TotalLoRAWeight:F2}");
            
            // Warn about high LoRA weights
            if (updatedCapabilities.TotalLoRAWeight > 2.0f)
            {
                updatedCapabilities.ModelWarnings.Add("High LoRA weight may cause instability or overtraining artifacts");
            }
            
            // Suggest temperature adjustments for LoRA usage
            if (updatedCapabilities.RecommendedDefaults.ContainsKey("Temperature"))
            {
                var baseTemp = (float)updatedCapabilities.RecommendedDefaults["Temperature"];
                var adjustedTemp = Math.Max(0.1f, baseTemp - (updatedCapabilities.TotalLoRAWeight * 0.1f)); // Lower temp for high LoRA weights
                updatedCapabilities.RecommendedDefaults["Temperature"] = adjustedTemp;
                updatedCapabilities.LoRAModifications["Temperature"] = new LoRAParameterModification
                {
                    ParameterName = "Temperature",
                    Type = ModificationType.Range,
                    NewDefaultValue = adjustedTemp,
                    ModificationDescription = $"Reduced from {baseTemp:F2} due to LoRA influence",
                    CausingLoRAs = appliedLoRAs.Where(l => l.IsEnabled).Select(l => l.Name).ToList()
                };
            }
        }
        
        Logger.LogDebug($"Model capabilities updated with LoRA information - {updatedCapabilities.LoRAModifications.Count} parameter modifications applied");
        return updatedCapabilities;
    }
    
    /// <summary>
    /// Apply specific LoRA modifications to model parameters
    /// </summary>
    private static void ApplyLoRAModifications(ModelCapabilities capabilities, AppliedLoRAInfo lora)
    {
        Logger.LogDebug($"Applying modifications for LoRA: {lora.Name} (weight: {lora.Weight:F2})");
        
        // Different LoRA types may affect different parameters
        switch (lora.AdapterType.ToLowerInvariant())
        {
            case "style":
            case "aesthetic":
                // Style LoRAs may make the model more sensitive to temperature
                ModifyParameterSensitivity(capabilities, "Temperature", 1.2f, lora);
                ModifyParameterSensitivity(capabilities, "TopP", 0.9f, lora);
                break;
                
            case "character":
            case "persona":
                // Character LoRAs may benefit from lower repetition penalty
                if (capabilities.AvailableParameters.ContainsKey("RepetitionPenalty"))
                {
                    var currentDefault = capabilities.RecommendedDefaults.GetValueOrDefault("RepetitionPenalty", 1.1f);
                    var newDefault = Math.Max(1.0f, (float)currentDefault - (lora.Weight * 0.05f));
                    capabilities.RecommendedDefaults["RepetitionPenalty"] = newDefault;
                }
                break;
                
            case "concept":
            case "subject":
                // Concept LoRAs may need higher guidance
                ModifyParameterSensitivity(capabilities, "TopK", 0.8f, lora);
                break;
                
            case "pose":
            case "composition":
                // Pose/composition LoRAs are often structural
                ModifyParameterSensitivity(capabilities, "PresencePenalty", 1.1f, lora);
                break;
        }
        
        // General LoRA effects based on weight
        if (lora.Weight > 1.0f)
        {
            // High weight LoRAs may need more conservative sampling
            capabilities.ModelWarnings.Add($"LoRA '{lora.Name}' has high weight ({lora.Weight:F2}) - consider reducing temperature");
        }
        
        if (lora.Rank < 16)
        {
            // Low rank LoRAs may be less expressive
            capabilities.ModelWarnings.Add($"LoRA '{lora.Name}' has low rank ({lora.Rank}) - may have limited expressiveness");
        }
        else if (lora.Rank > 64)
        {
            // High rank LoRAs may overfit
            capabilities.ModelWarnings.Add($"LoRA '{lora.Name}' has high rank ({lora.Rank}) - may cause overfitting");
        }
    }
    
    /// <summary>
    /// Modify parameter sensitivity based on LoRA influence
    /// </summary>
    private static void ModifyParameterSensitivity(ModelCapabilities capabilities, string paramName, float sensitivityMultiplier, AppliedLoRAInfo lora)
    {
        if (capabilities.AvailableParameters.ContainsKey(paramName))
        {
            var modKey = $"{paramName}_LoRA";
            if (capabilities.LoRAModifications.ContainsKey(modKey))
            {
                // Combine with existing modification
                capabilities.LoRAModifications[modKey].SensitivityMultiplier *= sensitivityMultiplier;
                capabilities.LoRAModifications[modKey].CausingLoRAs.Add(lora.Name);
            }
            else
            {
                capabilities.LoRAModifications[modKey] = new LoRAParameterModification
                {
                    ParameterName = paramName,
                    Type = ModificationType.Sensitivity,
                    SensitivityMultiplier = sensitivityMultiplier,
                    ModificationDescription = $"Parameter sensitivity modified by LoRA influence",
                    CausingLoRAs = new List<string> { lora.Name }
                };
            }
            
            // Update parameter description
            var param = capabilities.AvailableParameters[paramName];
            param.ModelSpecificDescription += $" (Modified by LoRA: {lora.Name})";
        }
    }
    
    /// <summary>
    /// Create AppliedLoRAInfo from a LoRADto for integration with capabilities
    /// </summary>
    public static AppliedLoRAInfo CreateLoRAInfo(object loraDto)
    {
        // This method will be called from the LoRAsViewModel to convert LoRADto to AppliedLoRAInfo
        // Using reflection to avoid circular dependencies
        var dto = loraDto;
        var type = dto.GetType();
        
        return new AppliedLoRAInfo
        {
            Id = (string)(type.GetProperty("Id")?.GetValue(dto) ?? ""),
            Name = (string)(type.GetProperty("Name")?.GetValue(dto) ?? ""),
            FilePath = (string)(type.GetProperty("FilePath")?.GetValue(dto) ?? ""),
            AdapterType = (string)(type.GetProperty("LoRAType")?.GetValue(dto) ?? ""),
            Weight = (float)(type.GetProperty("RecommendedWeight")?.GetValue(dto) ?? 0.8f),
            IsEnabled = true, // Assume enabled when applied
            Rank = (int)(type.GetProperty("Rank")?.GetValue(dto) ?? 16),
            Alpha = (int)(type.GetProperty("Alpha")?.GetValue(dto) ?? 16),
            TargetModules = (List<string>)(type.GetProperty("TargetModules")?.GetValue(dto) ?? new List<string>()),
            BaseModel = (string)(type.GetProperty("BaseModel")?.GetValue(dto) ?? ""),
            Description = (string)(type.GetProperty("Description")?.GetValue(dto) ?? ""),
            AppliedAt = DateTime.UtcNow
        };
    }
    
    #endregion
}
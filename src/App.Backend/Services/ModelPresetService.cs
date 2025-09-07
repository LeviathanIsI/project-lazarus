using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Lazarus.Shared;

namespace Lazarus.Backend.Services;

public interface IModelPresetService
{
    void EnsureFolders();
    void Save(ModelPreset preset);
    ModelPreset? Load(string name);
    IReadOnlyList<string> List();
}

public sealed class ModelPresetService : IModelPresetService
{
    private static string PresetDir => LazarusPaths.SystemData.ModelPresets;
    private static string PathFor(string name) => Path.Combine(PresetDir, $"{San(name)}.json");

    public void EnsureFolders()
    {
        Directory.CreateDirectory(PresetDir);
    }

    public void Save(ModelPreset preset)
    {
        if (preset is null) throw new ArgumentNullException(nameof(preset));
        if (string.IsNullOrWhiteSpace(preset.Name)) throw new ArgumentException("Preset Name is required", nameof(preset));

        EnsureFolders();
        var json = JsonSerializer.Serialize(preset, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(PathFor(preset.Name), json);
    }

    public ModelPreset? Load(string name)
    {
        var p = PathFor(name);
        if (!File.Exists(p)) return null;
        var json = File.ReadAllText(p);
        return JsonSerializer.Deserialize<ModelPreset>(json);
    }

    public IReadOnlyList<string> List()
    {
        EnsureFolders();
        return Directory.EnumerateFiles(PresetDir, "*.json").Select(f => Path.GetFileNameWithoutExtension(f)!).OrderBy(x => x).ToList();
    }

    public bool Delete(string name)
    {
        if (!Directory.Exists(PresetDir)) return false;
        var path = PathFor(name);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    private static string San(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Trim();
    }
}

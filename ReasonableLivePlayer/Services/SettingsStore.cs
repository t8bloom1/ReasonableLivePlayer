using System.IO;
using System.Text.Json;
using ReasonableLivePlayer.Models;

namespace ReasonableLivePlayer.Services;

public class SettingsStore
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ReasonableLivePlayer");

    private static readonly string SettingsPath =
        Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static void Save(SettingsData settings)
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    public static SettingsData Load()
    {
        if (!File.Exists(SettingsPath))
            return new SettingsData(null, 1, 0, 5);

        var json = File.ReadAllText(SettingsPath);
        var data = JsonSerializer.Deserialize<SettingsData>(json);
        if (data == null) return new SettingsData(null, 1, 0, 5);
        if (data.TransitionDelaySec < 0)
            data = data with { TransitionDelaySec = 5 };
        return data;
    }

    /// <summary>
    /// Save to a specific path (used for testing).
    /// </summary>
    public static void SaveTo(SettingsData settings, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (dir != null) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Load from a specific path (used for testing).
    /// </summary>
    public static SettingsData LoadFrom(string path)
    {
        if (!File.Exists(path))
            return new SettingsData(null, 1, 0, 5);

        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<SettingsData>(json);
        if (data == null) return new SettingsData(null, 1, 0, 5);
        if (data.TransitionDelaySec < 0)
            data = data with { TransitionDelaySec = 5 };
        return data;
    }
}

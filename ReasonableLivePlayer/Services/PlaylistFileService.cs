using System.IO;
using System.Text.Json;
using ReasonableLivePlayer.Models;

namespace ReasonableLivePlayer.Services;

public static class PlaylistFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Save(List<string> songPaths, string filePath)
    {
        var data = new PlaylistData(1, songPaths);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public static List<string> Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<PlaylistData>(json);
        return data?.Songs ?? [];
    }
}

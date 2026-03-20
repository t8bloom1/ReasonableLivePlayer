using System.IO;
using FsCheck;
using FsCheck.Xunit;
using ReasonableLivePlayer.Services;

namespace ReasonableLivePlayer.Tests;

/// <summary>
/// Feature: midi-driven-transport, Property 8: Playlist file round-trip
/// Validates: Requirements 7.1, 7.2
/// </summary>
public class PlaylistFileTests
{
    /// <summary>
    /// For any list of file path strings, saving as a .rlp file and then loading
    /// should produce an equivalent ordered list of songs.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Playlist_RoundTrip_Preserves_Paths()
    {
        return Prop.ForAll(
            Arb.From(
                from count in Gen.Choose(0, 20)
                from paths in Gen.ListOf(count,
                    from name in Arb.Generate<NonEmptyString>()
                    let sanitized = SanitizePath(name.Get)
                    where sanitized.Length > 0
                    from ext in Gen.Elements(".reason", ".rns")
                    select $@"C:\Music\{sanitized}{ext}")
                select paths.ToList()
            ),
            paths =>
            {
                var tempFile = Path.Combine(Path.GetTempPath(), $"rlp_test_{Guid.NewGuid()}.rlp");
                try
                {
                    PlaylistFileService.Save(paths, tempFile);
                    var loaded = PlaylistFileService.Load(tempFile);
                    return paths.SequenceEqual(loaded);
                }
                finally
                {
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                }
            });
    }

    private static string SanitizePath(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(input.Where(c => !invalid.Contains(c) && c != '.').ToArray()).Trim();
    }
}

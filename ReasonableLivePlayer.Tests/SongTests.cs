using FsCheck;
using FsCheck.Xunit;
using ReasonableLivePlayer.Models;

namespace ReasonableLivePlayer.Tests;

/// <summary>
/// Feature: midi-driven-transport, Property 5: DisplayName derived from file path
/// Validates: Requirements 5.1, 6.2
/// </summary>
public class SongTests
{
    /// <summary>
    /// For any valid file path ending in .reason or .rns,
    /// the Song's DisplayName should equal the filename without its extension.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property DisplayName_Is_FileNameWithoutExtension()
    {
        var extensions = new[] { ".reason", ".rns" };
        return Prop.ForAll(
            Arb.From(
                from name in Arb.Generate<NonEmptyString>()
                let sanitized = SanitizeFileName(name.Get)
                where sanitized.Length > 0
                from ext in Gen.Elements(extensions)
                from dir in Gen.Elements(@"C:\Music", @"D:\Songs", @"C:\Users\user\Documents")
                select (dir, sanitized, ext)
            ),
            tuple =>
            {
                var (dir, name, ext) = tuple;
                var filePath = System.IO.Path.Combine(dir, name + ext);
                var song = new Song { FilePath = filePath };
                return song.DisplayName == name;
            });
    }

    private static string SanitizeFileName(string input)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var sanitized = new string(input.Where(c => !invalid.Contains(c) && c != '.').ToArray()).Trim();
        return sanitized;
    }
}

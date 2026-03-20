using System.IO;
using FsCheck;
using FsCheck.Xunit;
using ReasonableLivePlayer.Models;
using ReasonableLivePlayer.Services;

namespace ReasonableLivePlayer.Tests;

/// <summary>
/// Feature: midi-driven-transport, Property 1: Settings round-trip persistence
/// Validates: Requirements 1.1, 1.3
/// </summary>
public class SettingsStoreTests
{
    /// <summary>
    /// For any valid settings (MIDI device name as non-empty string, channel 1-16,
    /// note number 0-127), saving to disk and then loading should produce an equivalent
    /// settings object.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Settings_RoundTrip_Preserves_Data()
    {
        return Prop.ForAll(
            Arb.From(
                from deviceName in Arb.Generate<NonEmptyString>()
                from channel in Gen.Choose(1, 16)
                from noteNumber in Gen.Choose(0, 127)
                from delaySec in Gen.Choose(0, 99)
                from loadLast in Arb.Generate<bool>()
                from alwaysOnTop in Arb.Generate<bool>()
                select new SettingsData(deviceName.Get, channel, noteNumber, delaySec,
                    LastPlaylistPath: null, LoadLastPlaylist: loadLast, AlwaysOnTop: alwaysOnTop)
            ),
            settings =>
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"rlp_test_{Guid.NewGuid()}.json");
                try
                {
                    SettingsStore.SaveTo(settings, tempPath);
                    var loaded = SettingsStore.LoadFrom(tempPath);
                    return loaded == settings;
                }
                finally
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
            });
    }
}

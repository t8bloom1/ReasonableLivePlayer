namespace ReasonableLivePlayer.Models;

public record SettingsData(
    string? MidiDeviceName,
    int MidiChannel,          // 1-16
    int EndNoteNumber,        // 0-127
    int TransitionDelaySec,   // seconds to wait between closing one song and opening the next
    string? LastPlaylistPath = null,
    bool LoadLastPlaylist = false,
    bool AlwaysOnTop = false
);

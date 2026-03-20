using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ReasonLivePlayer.Automation;
using ReasonLivePlayer.Models;
using ReasonLivePlayer.Services;
using ReasonLivePlayer.Views;

namespace ReasonLivePlayer.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ReasonBridge _bridge = new();
    private readonly MidiNoteListener _midiListener = new();
    private int _currentIndex = -1;
    private bool _isPlaylistActive;
    private bool _midiConnected;
    private bool _alwaysOnTop;
    private string _statusText = "Ready";
    private int _transitionDelaySec = 5;
    private bool _playlistDirty;

    public ObservableCollection<Song> Songs { get; } = [];

    public bool IsPlaylistActive
    {
        get => _isPlaylistActive;
        set { _isPlaylistActive = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlayPauseLabel)); OnPropertyChanged(nameof(PlayPauseTooltip)); }
    }

    public string PlayPauseLabel => IsPlaylistActive ? "⏸" : "▶";
    public string PlayPauseTooltip => IsPlaylistActive ? "Pause" : "Play";
    public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }

    public bool MidiConnected
    {
        get => _midiConnected;
        set { _midiConnected = value; OnPropertyChanged(); }
    }

    public bool AlwaysOnTop
    {
        get => _alwaysOnTop;
        set { _alwaysOnTop = value; OnPropertyChanged(); }
    }

    public bool PlaylistDirty
    {
        get => _playlistDirty;
        set { _playlistDirty = value; OnPropertyChanged(); }
    }

    public RelayCommand AddSongsCommand { get; }
    public RelayCommand RemoveSongCommand { get; }
    public RelayCommand PlayPauseCommand { get; }
    public RelayCommand SkipCommand { get; }
    public RelayCommand MoveUpCommand { get; }
    public RelayCommand MoveDownCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand SavePlaylistCommand { get; }
    public RelayCommand LoadPlaylistCommand { get; }
    public RelayCommand OpenHelpCommand { get; }
    public RelayCommand SelectSongCommand { get; }

    public MainViewModel()
    {
        _midiListener.EndNoteReceived += OnEndNoteReceived;
        _midiListener.ConnectionChanged += connected =>
            Application.Current?.Dispatcher.Invoke(() => MidiConnected = connected);

        Songs.CollectionChanged += (_, e) =>
        {
            PlaylistDirty = true;
            CommandManager.InvalidateRequerySuggested();
        };

        var settings = SettingsStore.Load();
        _transitionDelaySec = settings.TransitionDelaySec;
        AlwaysOnTop = settings.AlwaysOnTop;
        if (!string.IsNullOrEmpty(settings.MidiDeviceName))
            _midiListener.Connect(settings.MidiDeviceName, settings.MidiChannel - 1, settings.EndNoteNumber);

        if (settings.LoadLastPlaylist && !string.IsNullOrEmpty(settings.LastPlaylistPath)
            && System.IO.File.Exists(settings.LastPlaylistPath))
        {
            var paths = PlaylistFileService.Load(settings.LastPlaylistPath);
            foreach (var p in paths)
                Songs.Add(new Song { FilePath = p });
            StatusText = $"Loaded {Songs.Count} songs";
            PlaylistDirty = false;
        }

        AddSongsCommand = new RelayCommand(_ =>
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Reason Songs|*.reason;*.rns|All Files|*.*",
                Multiselect = true
            };
            if (dlg.ShowDialog() == true)
                foreach (var f in dlg.FileNames)
                    Songs.Add(new Song { FilePath = f });
        });

        RemoveSongCommand = new RelayCommand(p => { if (p is Song s) Songs.Remove(s); });

        PlayPauseCommand = new RelayCommand(_ =>
        {
            if (IsPlaylistActive) Pause();
            else Play();
        }, _ => Songs.Count > 0);

        SkipCommand = new RelayCommand(_ => Skip(), _ => Songs.Count > 0);

        MoveUpCommand = new RelayCommand(p =>
        {
            if (p is Song s) { var i = Songs.IndexOf(s); if (i > 0) Songs.Move(i, i - 1); }
        });

        MoveDownCommand = new RelayCommand(p =>
        {
            if (p is Song s) { var i = Songs.IndexOf(s); if (i < Songs.Count - 1) Songs.Move(i, i + 1); }
        });

        OpenSettingsCommand = new RelayCommand(_ =>
        {
            var dlg = new SettingsDialog { Owner = Application.Current.MainWindow };
            if (dlg.ShowDialog() == true)
            {
                _midiListener.Disconnect();
                var s = SettingsStore.Load();
                _transitionDelaySec = s.TransitionDelaySec;
                AlwaysOnTop = s.AlwaysOnTop;
                if (!string.IsNullOrEmpty(s.MidiDeviceName))
                    _midiListener.Connect(s.MidiDeviceName, s.MidiChannel - 1, s.EndNoteNumber);
            }
        });

        SavePlaylistCommand = new RelayCommand(_ => SavePlaylist(), _ => Songs.Count > 0);

        LoadPlaylistCommand = new RelayCommand(_ =>
        {
            var dlg = new OpenFileDialog { Filter = "RLP Playlist|*.rlp|All Files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                var paths = PlaylistFileService.Load(dlg.FileName);
                Songs.Clear();
                _currentIndex = -1;
                IsPlaylistActive = false;
                foreach (var p in paths)
                    Songs.Add(new Song { FilePath = p });
                StatusText = $"Loaded {Songs.Count} songs";
                PlaylistDirty = false;
                var cur = SettingsStore.Load();
                SettingsStore.Save(cur with { LastPlaylistPath = dlg.FileName });
            }
        });

        OpenHelpCommand = new RelayCommand(_ =>
        {
            var dlg = new HelpDialog { Owner = Application.Current.MainWindow };
            dlg.ShowDialog();
        });

        SelectSongCommand = new RelayCommand(p =>
        {
            if (p is Song s && !IsPlaylistActive)
            {
                var idx = Songs.IndexOf(s);
                if (idx >= 0)
                {
                    _currentIndex = idx;
                    SetActiveSong(idx);
                    StatusText = $"Next: {s.DisplayName}";
                }
            }
        }, _ => !IsPlaylistActive);
    }

    public bool PromptSaveIfDirty()
    {
        if (!PlaylistDirty || Songs.Count == 0) return true;

        var result = MessageBox.Show(
            "The playlist has been modified. Save before closing?",
            "Save Playlist",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel) return false;
        if (result == MessageBoxResult.Yes) SavePlaylist();
        return true;
    }

    /// <summary>
    /// Disconnects MIDI and cleans up resources. Call on app shutdown.
    /// </summary>
    public void Cleanup()
    {
        _midiListener.Dispose();
    }

    private void SavePlaylist()
    {
        var dlg = new SaveFileDialog { Filter = "RLP Playlist|*.rlp", DefaultExt = ".rlp" };
        if (dlg.ShowDialog() == true)
        {
            PlaylistFileService.Save(Songs.Select(s => s.FilePath).ToList(), dlg.FileName);
            PlaylistDirty = false;
            var cur = SettingsStore.Load();
            SettingsStore.Save(cur with { LastPlaylistPath = dlg.FileName });
        }
    }

    private void OnEndNoteReceived()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (!IsPlaylistActive) return;
            AdvanceToNext();
        });
    }

    private async void Play()
    {
        if (Songs.Count == 0) return;
        if (_currentIndex < 0) _currentIndex = 0;

        SetActiveSong(_currentIndex);
        var song = Songs[_currentIndex];
        StatusText = $"Opening: {song.DisplayName}";
        await _bridge.OpenSongAsync(song.FilePath);
        IsPlaylistActive = true;
        StatusText = $"Playing: {song.DisplayName}";
    }

    private void Pause()
    {
        IsPlaylistActive = false;
        StatusText = "Paused";
    }

    private void Skip()
    {
        // Close current song, then advance without closing again
        CloseCurrent();
        IsPlaylistActive = false;
        AdvanceToNext(alreadyClosed: true);
    }

    /// <summary>
    /// Advances to the next song.
    /// </summary>
    /// <param name="alreadyClosed">If true, skip closing the current song (caller already did it).</param>
    private async void AdvanceToNext(bool alreadyClosed = false)
    {
        if (!alreadyClosed)
            CloseCurrent();

        if (_currentIndex < Songs.Count - 1)
        {
            _currentIndex++;
            SetActiveSong(_currentIndex);
            var song = Songs[_currentIndex];
            StatusText = $"Opening: {song.DisplayName}";
            await Task.Delay(_transitionDelaySec * 1000);
            await _bridge.OpenSongAsync(song.FilePath);
            StatusText = $"Ready: {song.DisplayName}";
        }
        else
        {
            ClearActiveSong();
            _currentIndex = -1;
            IsPlaylistActive = false;
            StatusText = "Set complete";
        }
    }

    private void CloseCurrent()
    {
        if (_currentIndex >= 0 && _currentIndex < Songs.Count)
            _bridge.CloseSong(Songs[_currentIndex].FilePath);
    }

    private void SetActiveSong(int index)
    {
        foreach (var s in Songs) s.IsActive = false;
        if (index >= 0 && index < Songs.Count)
            Songs[index].IsActive = true;
    }

    private void ClearActiveSong()
    {
        foreach (var s in Songs) s.IsActive = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

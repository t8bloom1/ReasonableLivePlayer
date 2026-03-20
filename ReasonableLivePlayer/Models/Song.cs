using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace ReasonableLivePlayer.Models;

public class Song : INotifyPropertyChanged
{
    private bool _isActive;

    public string FilePath { get; set; } = string.Empty;
    public string DisplayName => Path.GetFileNameWithoutExtension(FilePath);
    public bool FileExists => File.Exists(FilePath);

    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

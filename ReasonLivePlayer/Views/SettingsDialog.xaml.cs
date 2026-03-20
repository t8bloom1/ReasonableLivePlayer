using System.Windows;
using ReasonLivePlayer.Automation;
using ReasonLivePlayer.Models;
using ReasonLivePlayer.Services;

namespace ReasonLivePlayer.Views;

public partial class SettingsDialog : Window
{
    public SettingsDialog()
    {
        InitializeComponent();

        // Populate MIDI devices
        var devices = MidiNoteListener.GetAvailableDevices();
        DeviceCombo.ItemsSource = devices;

        // Populate channel 1-16
        ChannelCombo.ItemsSource = Enumerable.Range(1, 16).ToList();

        // Populate note 0-127
        NoteCombo.ItemsSource = Enumerable.Range(0, 128).ToList();

        // Load current settings
        var settings = SettingsStore.Load();
        if (settings.MidiDeviceName != null && devices.Contains(settings.MidiDeviceName))
            DeviceCombo.SelectedItem = settings.MidiDeviceName;
        ChannelCombo.SelectedItem = settings.MidiChannel;
        NoteCombo.SelectedItem = settings.EndNoteNumber;
        DelayTextBox.Text = settings.TransitionDelaySec.ToString();
        LoadLastPlaylistCheck.IsChecked = settings.LoadLastPlaylist;
        AlwaysOnTopCheck.IsChecked = settings.AlwaysOnTop;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Validate transition delay: must be integer 0-99
        if (!int.TryParse(DelayTextBox.Text, out int delay) || delay < 0 || delay > 99)
        {
            MessageBox.Show("Transition delay must be a number between 0 and 99.",
                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            DelayTextBox.Focus();
            return;
        }

        var data = new SettingsData(
            DeviceCombo.SelectedItem as string,
            ChannelCombo.SelectedItem is int ch ? ch : 1,
            NoteCombo.SelectedItem is int note ? note : 0,
            delay,
            LastPlaylistPath: SettingsStore.Load().LastPlaylistPath, // preserve existing path
            LoadLastPlaylist: LoadLastPlaylistCheck.IsChecked == true,
            AlwaysOnTop: AlwaysOnTopCheck.IsChecked == true
        );
        SettingsStore.Save(data);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

using System.Runtime.InteropServices;

namespace ReasonableLivePlayer.Automation;

/// <summary>
/// Listens on a MIDI input device for a specific note-on message
/// matching a configured channel and note number.
/// </summary>
public class MidiNoteListener : IDisposable
{
    [DllImport("winmm.dll")] private static extern int midiInGetNumDevs();
    [DllImport("winmm.dll", EntryPoint = "midiInGetDevCapsW")] private static extern int midiInGetDevCaps(int deviceId, ref MidiInCaps caps, int size);
    [DllImport("winmm.dll")] private static extern int midiInOpen(out IntPtr handle, int deviceId, MidiInProc callback, IntPtr instance, int flags);
    [DllImport("winmm.dll")] private static extern int midiInStart(IntPtr handle);
    [DllImport("winmm.dll")] private static extern int midiInStop(IntPtr handle);
    [DllImport("winmm.dll")] private static extern int midiInClose(IntPtr handle);

    private delegate void MidiInProc(IntPtr handle, int msg, IntPtr instance, IntPtr param1, IntPtr param2);
    private const int CALLBACK_FUNCTION = 0x30000;
    private const int MIM_DATA = 0x3C3;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MidiInCaps
    {
        public short mid;
        public short pid;
        public int driverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string name;
        public int support;
    }

    private IntPtr _midiHandle;
    private MidiInProc? _callback;

    public string? DeviceName { get; private set; }
    public int Channel { get; private set; }     // 0-15 (MIDI channel - 1)
    public int NoteNumber { get; private set; }  // 0-127
    public bool IsConnected { get; private set; }

    public event Action? EndNoteReceived;
    public event Action<bool>? ConnectionChanged;

    public bool Connect(string deviceName, int channel, int noteNumber)
    {
        Disconnect();
        DeviceName = deviceName;
        Channel = channel;
        NoteNumber = noteNumber;

        int count = midiInGetNumDevs();
        for (int i = 0; i < count; i++)
        {
            var caps = new MidiInCaps();
            if (midiInGetDevCaps(i, ref caps, Marshal.SizeOf<MidiInCaps>()) != 0) continue;
            if (!string.Equals(caps.name, deviceName, StringComparison.OrdinalIgnoreCase)) continue;

            _callback = MidiCallback;
            if (midiInOpen(out _midiHandle, i, _callback, IntPtr.Zero, CALLBACK_FUNCTION) == 0)
            {
                midiInStart(_midiHandle);
                IsConnected = true;
                ConnectionChanged?.Invoke(true);
                return true;
            }
        }

        IsConnected = false;
        ConnectionChanged?.Invoke(false);
        return false;
    }

    public void Disconnect()
    {
        if (_midiHandle != IntPtr.Zero)
        {
            midiInStop(_midiHandle);
            midiInClose(_midiHandle);
            _midiHandle = IntPtr.Zero;
        }
        if (IsConnected)
        {
            IsConnected = false;
            ConnectionChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// Determines if a raw MIDI message matches the configured channel and note
    /// with non-zero velocity. Extracted for testability.
    /// </summary>
    public static bool IsMatchingNoteOn(int midiData, int channel, int noteNumber)
    {
        int status = midiData & 0xFF;
        int note = (midiData >> 8) & 0x7F;
        int velocity = (midiData >> 16) & 0x7F;

        // Note-on is 0x90-0x9F; channel is lower nibble
        int messageType = status & 0xF0;
        int messageChannel = status & 0x0F;

        return messageType == 0x90
            && messageChannel == channel
            && note == noteNumber
            && velocity > 0;
    }

    private void MidiCallback(IntPtr handle, int msg, IntPtr instance, IntPtr param1, IntPtr param2)
    {
        if (msg != MIM_DATA) return;
        int data = param1.ToInt32();
        if (IsMatchingNoteOn(data, Channel, NoteNumber))
            EndNoteReceived?.Invoke();
    }

    public static List<string> GetAvailableDevices()
    {
        var devices = new List<string>();
        int count = midiInGetNumDevs();
        for (int i = 0; i < count; i++)
        {
            var caps = new MidiInCaps();
            if (midiInGetDevCaps(i, ref caps, Marshal.SizeOf<MidiInCaps>()) == 0)
                devices.Add(caps.name);
        }
        return devices;
    }

    public void Dispose()
    {
        Disconnect();
        GC.SuppressFinalize(this);
    }

    ~MidiNoteListener() => Disconnect();
}

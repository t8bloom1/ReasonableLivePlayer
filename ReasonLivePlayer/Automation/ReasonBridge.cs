using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ReasonLivePlayer.Automation;

/// <summary>
/// Controls Reason DAW via Windows automation.
/// Tracks which songs RLP opened so it only closes those specific windows.
/// Uses window-snapshot diffing to reliably detect new Reason windows
/// even when other Reason files are already open.
/// </summary>
public class ReasonBridge
{
    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, char[] lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const byte VK_MENU = 0x12;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private const uint WM_CLOSE = 0x0010;

    private readonly Dictionary<string, IntPtr> _openWindows = new(StringComparer.OrdinalIgnoreCase);

    public async Task OpenSongAsync(string filePath)
    {
        // Force RLP to the foreground before shell execute.
        // Windows blocks SetForegroundWindow from background processes,
        // so we use AttachThreadInput + ALT key trick to bypass the restriction.
        ForceForeground(Process.GetCurrentProcess().MainWindowHandle);

        var beforeHandles = GetAllVisibleWindowHandles();

        var psi = new ProcessStartInfo(filePath) { UseShellExecute = true };
        Process.Start(psi);

        IntPtr hwnd = IntPtr.Zero;
        var songName = Path.GetFileNameWithoutExtension(filePath);

        for (int i = 0; i < 30; i++)
        {
            await Task.Delay(500);
            hwnd = FindNewWindowForSong(beforeHandles, songName);
            if (hwnd != IntPtr.Zero) break;
            if (i >= 10)
            {
                hwnd = FindAnyNewReasonWindow(beforeHandles);
                if (hwnd != IntPtr.Zero) break;
            }
        }

        if (hwnd != IntPtr.Zero)
            _openWindows[filePath] = hwnd;
    }

    public void CloseSong(string filePath)
    {
        if (_openWindows.TryGetValue(filePath, out var tracked) && IsWindow(tracked))
        {
            PostMessage(tracked, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _openWindows.Remove(filePath);
            return;
        }

        var songName = Path.GetFileNameWithoutExtension(filePath);
        var hwnd = FindWindowByTitle(songName);
        if (hwnd != IntPtr.Zero)
        {
            PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _openWindows.Remove(filePath);
        }
    }

    /// <summary>
    /// Forces a window to the foreground even when another app has focus.
    /// Attaches to the foreground thread and simulates ALT to bypass Windows' lock.
    /// </summary>
    private static void ForceForeground(IntPtr targetHwnd)
    {
        if (targetHwnd == IntPtr.Zero) return;

        var fgHwnd = GetForegroundWindow();
        if (fgHwnd == targetHwnd) return;

        uint curThread = GetCurrentThreadId();
        uint fgThread = GetWindowThreadProcessId(fgHwnd, out _);

        bool attached = false;
        if (curThread != fgThread)
            attached = AttachThreadInput(curThread, fgThread, true);

        // ALT press/release tricks Windows into allowing SetForegroundWindow
        keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
        keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

        SetForegroundWindow(targetHwnd);

        if (attached)
            AttachThreadInput(curThread, fgThread, false);
    }

    private static HashSet<IntPtr> GetAllVisibleWindowHandles()
    {
        var handles = new HashSet<IntPtr>();
        EnumWindows((hwnd, _) =>
        {
            if (IsWindowVisible(hwnd)) handles.Add(hwnd);
            return true;
        }, IntPtr.Zero);
        return handles;
    }

    private static IntPtr FindNewWindowForSong(HashSet<IntPtr> before, string songName)
    {
        if (string.IsNullOrEmpty(songName)) return IntPtr.Zero;
        IntPtr found = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) || before.Contains(hwnd)) return true;
            if (GetWindowTitle(hwnd).Contains(songName, StringComparison.OrdinalIgnoreCase))
            { found = hwnd; return false; }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    private static IntPtr FindAnyNewReasonWindow(HashSet<IntPtr> before)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) || before.Contains(hwnd)) return true;
            GetWindowThreadProcessId(hwnd, out uint pid);
            try
            {
                var proc = Process.GetProcessById((int)pid);
                if (proc.ProcessName.Contains("Reason", StringComparison.OrdinalIgnoreCase))
                { found = hwnd; return false; }
            }
            catch { }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    private static IntPtr FindWindowByTitle(string text)
    {
        if (string.IsNullOrEmpty(text)) return IntPtr.Zero;
        IntPtr found = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) return true;
            if (GetWindowTitle(hwnd).Contains(text, StringComparison.OrdinalIgnoreCase))
            { found = hwnd; return false; }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        int len = GetWindowTextLength(hwnd);
        if (len <= 0) return string.Empty;
        var buf = new char[len + 1];
        GetWindowText(hwnd, buf, buf.Length);
        return new string(buf, 0, len);
    }
}

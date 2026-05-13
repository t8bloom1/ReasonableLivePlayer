using Microsoft.Win32;

namespace ReasonableLivePlayer.Services;

public static class FileAssociationService
{
    private const string Extension = ".rlp";
    private const string ProgId = "ReasonableLivePlayer.Playlist";
    private const string Description = "Reasonable Live Player Playlist";

    public static void EnsureRegistered()
    {
        try
        {
            using var extKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{Extension}");
            if (extKey != null) return; // Already registered

            Register();
        }
        catch (Exception)
        {
            // Permission failure or other registry error — continue startup silently
        }
    }

    private static void Register()
    {
        var exePath = Environment.ProcessPath ?? "";

        // HKCU\Software\Classes\.rlp → ProgId
        using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{Extension}"))
            key.SetValue("", ProgId);

        // HKCU\Software\Classes\ReasonableLivePlayer.Playlist → Description
        using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}"))
            key.SetValue("", Description);

        // DefaultIcon → "<exePath>",0
        using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\DefaultIcon"))
            key.SetValue("", $"\"{exePath}\",0");

        // shell\open\command → "<exePath>" "%1"
        using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\open\command"))
            key.SetValue("", $"\"{exePath}\" \"%1\"");
    }
}

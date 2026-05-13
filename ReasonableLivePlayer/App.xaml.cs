using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using ReasonableLivePlayer.Services;

namespace ReasonableLivePlayer;

public partial class App : Application
{
    private const string MutexName = "ReasonableLivePlayer_SingleInstance";
    private const string PipeName = "ReasonableLivePlayer_Pipe";
    private static Mutex? _mutex;
    private static bool _ownsMutex;

    public static string? StartupPlaylistPath { get; private set; }

    /// <summary>
    /// Raised when a second instance sends a playlist path to this instance.
    /// </summary>
    public static event Action<string>? PlaylistReceived;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show(args.Exception.ToString(), "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // Parse command-line arg — use as-is, just trim quotes
        string? playlistArg = null;
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            var candidate = args[1].Trim().Trim('"');
            if (candidate.EndsWith(".rlp", StringComparison.OrdinalIgnoreCase))
                playlistArg = candidate;
        }

        // Single-instance check
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        _ownsMutex = createdNew;

        if (!createdNew)
        {
            // Another instance is running — send the playlist path and exit
            if (!string.IsNullOrEmpty(playlistArg))
                SendPathToExistingInstance(playlistArg);
            _mutex.Dispose();
            _mutex = null;
            Shutdown();
            return;
        }

        StartupPlaylistPath = playlistArg;
        FileAssociationService.EnsureRegistered();

        // Start listening for messages from other instances
        StartPipeServer();

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex && _mutex != null)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
            _mutex = null;
        }
        base.OnExit(e);
    }

    private static void SendPathToExistingInstance(string path)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(3000); // 3 second timeout
            using var writer = new StreamWriter(client);
            writer.Write(path);
            writer.Flush();
        }
        catch
        {
            // If we can't communicate, just exit silently
        }
    }

    private void StartPipeServer()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                    await server.WaitForConnectionAsync();
                    using var reader = new StreamReader(server);
                    var path = await reader.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(path))
                    {
                        Dispatcher.Invoke(() => PlaylistReceived?.Invoke(path));
                    }
                }
                catch
                {
                    // Pipe error — restart listener
                    await Task.Delay(100);
                }
            }
        });
    }
}

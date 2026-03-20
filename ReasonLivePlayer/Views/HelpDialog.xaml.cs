using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace ReasonLivePlayer.Views;

public partial class HelpDialog : Window
{
    public HelpDialog()
    {
        InitializeComponent();

        var asm = Assembly.GetExecutingAssembly();
        var version = asm.GetName().Version;
        VersionRun.Text = $"Version: {version?.Major}.{version?.Minor}.{version?.Build}";

        var exePath = Path.Combine(AppContext.BaseDirectory, "ReasonableLivePlayer.exe");
        var buildDate = System.IO.File.Exists(exePath)
            ? System.IO.File.GetLastWriteTime(exePath)
            : DateTime.Now;
        BuildDateRun.Text = $"Build date: {buildDate:yyyy-MM-dd}";

        RepoLink.NavigateUri = new Uri("https://github.com/t8bloom1/ReasonableLivePlayer");
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}

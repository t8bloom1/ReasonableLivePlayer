using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ReasonLivePlayer.Models;
using ReasonLivePlayer.ViewModels;

namespace ReasonLivePlayer;

public partial class MainWindow : Window
{
    private Point _dragStart;
    private Song? _draggedSong;
    private DropIndicatorAdorner? _dropAdorner;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        LoadIcon();
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (!vm.PromptSaveIfDirty())
        {
            e.Cancel = true;
            return;
        }
        vm.Cleanup();
    }

    private void LoadIcon()
    {
        try
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var iconPath = Path.Combine(exeDir, "icon.ico");
            if (File.Exists(iconPath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                Icon = bitmap;
            }
        }
        catch
        {
            // No icon is fine — don't crash the app over it
        }
    }

    private void Playlist_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
        if (e.OriginalSource is FrameworkElement fe && fe.DataContext is Song s)
            _draggedSong = s;
    }

    private void Playlist_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedSong == null) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _dragStart.X) < 10 && Math.Abs(pos.Y - _dragStart.Y) < 10) return;
        DragDrop.DoDragDrop((ListBox)sender, _draggedSong, DragDropEffects.Move);
        _draggedSong = null;
        RemoveDropAdorner();
    }

    private void Playlist_Drop(object sender, DragEventArgs e)
    {
        RemoveDropAdorner();

        if (DataContext is not MainViewModel vm) return;

        // Handle file drops from Windows Explorer
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                foreach (var f in files)
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext is ".reason" or ".rns")
                        vm.Songs.Add(new Song { FilePath = f });
                }
            }
            return;
        }

        // Handle internal drag-reorder
        if (e.Data.GetData(typeof(Song)) is not Song dropped) return;
        var target = GetSongAtPoint(sender as ListBox, e.GetPosition((ListBox)sender));
        if (target == null || target == dropped) return;

        int oldIdx = vm.Songs.IndexOf(dropped);
        int newIdx = vm.Songs.IndexOf(target);
        vm.Songs.Move(oldIdx, newIdx);
    }

    private void Playlist_DragOver(object sender, DragEventArgs e)
    {
        // Accept file drops from Explorer
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
            RemoveDropAdorner();
            return;
        }

        // Show drop indicator for internal reorder
        if (sender is not ListBox listBox) return;
        e.Effects = DragDropEffects.Move;
        e.Handled = true;

        var pos = e.GetPosition(listBox);
        var targetItem = GetListBoxItemAtPoint(listBox, pos);

        if (targetItem != null)
        {
            // Determine if we're in the top or bottom half of the item
            var itemPos = e.GetPosition(targetItem);
            bool insertBelow = itemPos.Y > targetItem.ActualHeight / 2;

            var itemTransform = targetItem.TransformToAncestor(listBox);
            var itemTopInList = itemTransform.Transform(new Point(0, 0)).Y;
            double lineY = insertBelow ? itemTopInList + targetItem.ActualHeight : itemTopInList;

            ShowDropAdorner(listBox, lineY);
        }
        else
        {
            RemoveDropAdorner();
        }
    }

    private void Playlist_DragLeave(object sender, DragEventArgs e)
    {
        RemoveDropAdorner();
    }

    private void ShowDropAdorner(ListBox listBox, double y)
    {
        var layer = AdornerLayer.GetAdornerLayer(listBox);
        if (layer == null) return;

        if (_dropAdorner != null && _dropAdorner.AdornedElement == listBox)
        {
            _dropAdorner.LineY = y;
            _dropAdorner.InvalidateVisual();
        }
        else
        {
            RemoveDropAdorner();
            _dropAdorner = new DropIndicatorAdorner(listBox, y);
            layer.Add(_dropAdorner);
        }
    }

    private void RemoveDropAdorner()
    {
        if (_dropAdorner != null)
        {
            var layer = AdornerLayer.GetAdornerLayer(_dropAdorner.AdornedElement);
            layer?.Remove(_dropAdorner);
            _dropAdorner = null;
        }
    }

    private static Song? GetSongAtPoint(ListBox? list, Point pt)
    {
        if (list == null) return null;
        var element = list.InputHitTest(pt) as FrameworkElement;
        while (element != null && element.DataContext is not Song)
            element = element.Parent as FrameworkElement;
        return element?.DataContext as Song;
    }

    private static ListBoxItem? GetListBoxItemAtPoint(ListBox list, Point pt)
    {
        var element = list.InputHitTest(pt) as DependencyObject;
        while (element != null && element is not ListBoxItem)
            element = VisualTreeHelper.GetParent(element);
        return element as ListBoxItem;
    }
}

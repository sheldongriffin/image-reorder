using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ImageReorder.Services;
using ImageReorder.ViewModels;

namespace ImageReorder;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, (_, _) =>
        {
            if (DataContext is MainViewModel viewModel && viewModel.OpenFolderCommand.CanExecute(null))
            {
                viewModel.OpenFolderCommand.Execute(null);
            }
        }));
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private void OnWindowDragOver(object sender, DragEventArgs e)
    {
        var canDrop = MainViewModel.CanAcceptDrop(e.Data);
        e.Effects = canDrop ? DragDropEffects.Copy : DragDropEffects.None;
        DropHint.Visibility = canDrop ? Visibility.Visible : Visibility.Collapsed;
        e.Handled = true;
    }

    private void OnWindowDragLeave(object sender, DragEventArgs e)
    {
        var position = e.GetPosition(this);
        if (position.X < 0 || position.Y < 0 || position.X > ActualWidth || position.Y > ActualHeight)
        {
            DropHint.Visibility = Visibility.Collapsed;
        }
    }

    private void OnWindowDrop(object sender, DragEventArgs e)
    {
        DropHint.Visibility = Visibility.Collapsed;
        ViewModel.TryAcceptDrop(e.Data);
        e.Handled = true;
    }

    private void OnThumbnailDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not FrameworkElement { DataContext: ImageEntry entry })
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(entry.FilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not open image", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();

    private void OnOpenLogFileClick(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(AppLog.LogFilePath))
        {
            AppLog.Info("Log file opened from the Help menu.");
        }

        Reveal(AppLog.LogFilePath);
    }

    private void OnOpenLogFolderClick(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(AppLog.LogDirectory);
        Reveal(AppLog.LogDirectory);
    }

    private void Reveal(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Could not open {path}.", ex);
            MessageBox.Show(path, "Log location", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

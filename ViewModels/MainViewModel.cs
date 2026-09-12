using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ImageReorder.Services;

namespace ImageReorder.ViewModels;

internal sealed class MainViewModel : ObservableObject
{
    private string? _folderPath;
    private string _statusText = "Drop a folder of images to begin.";
    private bool _isBusy;
    private ImageEntry? _draggingItem;

    public MainViewModel()
    {
        Images.CollectionChanged += OnImagesCollectionChanged;
        OpenFolderCommand = new RelayCommand(OpenFolder, () => !IsBusy);
        ApplyNamesCommand = new RelayCommand(ApplyNames, () => CanApply);
        ClearFolderCommand = new RelayCommand(ClearFolder, () => HasFolder && !IsBusy);
    }

    public ObservableCollection<ImageEntry> Images { get; } = [];

    public ICommand OpenFolderCommand { get; }

    public ICommand ApplyNamesCommand { get; }

    public ICommand ClearFolderCommand { get; }

    public string? FolderPath
    {
        get => _folderPath;
        private set
        {
            if (SetProperty(ref _folderPath, value))
            {
                OnPropertyChanged(nameof(FolderName));
                OnPropertyChanged(nameof(HasFolder));
                OnPropertyChanged(nameof(HeaderSubtitle));
            }
        }
    }

    public string FolderName =>
        string.IsNullOrWhiteSpace(FolderPath) ? "No folder selected" : Path.GetFileName(FolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    public bool HasFolder => !string.IsNullOrWhiteSpace(FolderPath);

    public string HeaderSubtitle =>
        HasFolder
            ? $"{Images.Count} image{(Images.Count == 1 ? string.Empty : "s")} · {PendingRenameCount} to rename"
            : "Traditional desktop app — drag a folder, arrange thumbnails, then apply IMG_xxxx names";

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RelayCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanApply => HasFolder && !IsBusy && Images.Count > 0 && PendingRenameCount > 0;

    public int PendingRenameCount => Images.Count(image => image.NeedsRename);

    public bool TryAcceptDrop(IDataObject data)
    {
        var folder = TryGetDroppedFolder(data);
        if (folder is null)
        {
            return false;
        }

        LoadFolder(folder);
        return true;
    }

    public static bool CanAcceptDrop(IDataObject data) => TryGetDroppedFolder(data) is not null;

    public void LoadFolder(string folderPath)
    {
        if (IsBusy)
        {
            return;
        }

        if (!Directory.Exists(folderPath))
        {
            StatusText = "That folder no longer exists.";
            return;
        }

        if (HasFolder && PendingRenameCount > 0)
        {
            var proceed = MessageBox.Show(
                "You have an unapplied order. Load a different folder anyway?",
                "Image Reorder",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (proceed != MessageBoxResult.Yes)
            {
                return;
            }
        }

        var files = Directory.EnumerateFiles(folderPath)
            .Where(ThumbnailLoader.IsSupportedImage)
            .OrderBy(path => Path.GetFileName(path) ?? string.Empty, Comparer<string>.Create(NaturalSort.CompareFileNames))
            .ToList();

        Images.Clear();
        FolderPath = folderPath;

        foreach (var file in files)
        {
            Images.Add(new ImageEntry(file));
        }

        RefreshSequenceNumbers();
        QueueThumbnailLoads();

        AppLog.Info($"Loaded {Images.Count} image(s) from {folderPath}.");

        StatusText = Images.Count == 0
            ? "That folder does not contain supported images (JPG, PNG, GIF, BMP, TIFF)."
            : $"Loaded {Images.Count} image{(Images.Count == 1 ? string.Empty : "s")}. Drag thumbnails to set a new order.";

        RelayCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(CanApply));
    }

    // TODO: Support selecting more than one file and moving the whole selection at once.
    // First pass only moves a single thumbnail.
    public void MoveItem(ImageEntry item, int targetIndex)
    {
        var currentIndex = Images.IndexOf(item);
        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= Images.Count || currentIndex == targetIndex)
        {
            AppLog.Trace($"Skipped move of {item.FileName}: from {currentIndex} to {targetIndex} of {Images.Count}.");
            return;
        }

        AppLog.Trace($"Moving {item.FileName} from index {currentIndex} to {targetIndex}.");
        Images.Move(currentIndex, targetIndex);
        StatusText = $"Moved {item.FileName} to position {item.SequenceNumber}.";
    }

    public void BeginDrag(ImageEntry item)
    {
        _draggingItem = item;
        item.IsDragging = true;
    }

    public void EndDrag()
    {
        if (_draggingItem is null)
        {
            return;
        }

        _draggingItem.IsDragging = false;
        _draggingItem = null;
    }

    public ImageEntry? DraggingItem => _draggingItem;

    private void ApplyNames()
    {
        if (!CanApply || FolderPath is null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            $"Rename {PendingRenameCount} file{(PendingRenameCount == 1 ? string.Empty : "s")} to IMG_xxxx in the current thumbnail order?\n\nThis changes names on disk in:\n{FolderPath}",
            "Apply image names",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.Cancel);

        if (confirm != MessageBoxResult.OK)
        {
            return;
        }

        IsBusy = true;
        StatusText = "Renaming files…";

        try
        {
            var requests = Images
                .Select(image => new RenameRequest(image.FilePath, image.PlannedName))
                .ToList();

            foreach (var (fromName, toName) in CollisionSafeRenamer.Plan(requests))
            {
                AppLog.Info($"Rename planned: {fromName} -> {toName}");
            }

            CollisionSafeRenamer.Execute(requests);

            foreach (var image in Images)
            {
                var newPath = Path.Combine(FolderPath, image.PlannedName);
                image.ApplyRenamedPath(newPath);
            }

            AppLog.Info("Rename completed.");
            StatusText = "Files renamed to match the thumbnail order.";
        }
        catch (Exception ex)
        {
            AppLog.Error("Rename failed.", ex);
            StatusText = $"Rename failed: {ex.Message}";
            MessageBox.Show(
                ex.Message,
                "Rename failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(HeaderSubtitle));
            OnPropertyChanged(nameof(CanApply));
            RelayCommand.RaiseCanExecuteChanged();
        }
    }

    private void OpenFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Choose a folder of images",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            LoadFolder(dialog.FolderName);
        }
    }

    private void ClearFolder()
    {
        if (PendingRenameCount > 0)
        {
            var proceed = MessageBox.Show(
                "You have an unapplied order. Clear the current folder anyway?",
                "Image Reorder",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (proceed != MessageBoxResult.Yes)
            {
                return;
            }
        }

        Images.Clear();
        FolderPath = null;
        StatusText = "Drop a folder of images to begin.";
        RelayCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(CanApply));
    }

    private void QueueThumbnailLoads()
    {
        var dispatcher = Application.Current.Dispatcher;
        foreach (var image in Images.ToList())
        {
            dispatcher.BeginInvoke(() => image.LoadThumbnail(), DispatcherPriority.Background);
        }
    }

    private void OnImagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshSequenceNumbers();
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(CanApply));
        RelayCommand.RaiseCanExecuteChanged();
    }

    private void RefreshSequenceNumbers()
    {
        for (var i = 0; i < Images.Count; i++)
        {
            Images[i].SequenceNumber = i + 1;
        }
    }

    private static string? TryGetDroppedFolder(IDataObject data)
    {
        if (!data.GetDataPresent(DataFormats.FileDrop))
        {
            return null;
        }

        if (data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0)
        {
            return null;
        }

        if (paths.Length == 1 && Directory.Exists(paths[0]))
        {
            return paths[0];
        }

        var directories = paths
            .Select(path => Directory.Exists(path) ? path : Path.GetDirectoryName(path))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return directories.Count == 1 ? directories[0] : null;
    }
}

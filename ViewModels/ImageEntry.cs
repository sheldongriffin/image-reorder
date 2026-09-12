using System.IO;
using System.Windows.Media;
using ImageReorder.Services;

namespace ImageReorder.ViewModels;

internal sealed class ImageEntry : ObservableObject
{
    private ImageSource? _thumbnail;
    private int _sequenceNumber;
    private bool _isDragging;
    private string _filePath;

    public ImageEntry(string filePath)
    {
        _filePath = filePath;
        FileName = Path.GetFileName(filePath);
        Extension = Path.GetExtension(filePath);
    }

    public string FilePath
    {
        get => _filePath;
        private set
        {
            if (SetProperty(ref _filePath, value))
            {
                FileName = Path.GetFileName(value);
                Extension = Path.GetExtension(value);
                OnPropertyChanged(nameof(FileName));
                OnPropertyChanged(nameof(Extension));
                OnPropertyChanged(nameof(PlannedName));
                OnPropertyChanged(nameof(NeedsRename));
            }
        }
    }

    public string FileName { get; private set; }

    public string Extension { get; private set; }

    public ImageSource? Thumbnail
    {
        get => _thumbnail;
        set => SetProperty(ref _thumbnail, value);
    }

    public int SequenceNumber
    {
        get => _sequenceNumber;
        set
        {
            if (SetProperty(ref _sequenceNumber, value))
            {
                OnPropertyChanged(nameof(PlannedName));
                OnPropertyChanged(nameof(BadgeText));
                OnPropertyChanged(nameof(NeedsRename));
            }
        }
    }

    public bool IsDragging
    {
        get => _isDragging;
        set => SetProperty(ref _isDragging, value);
    }

    public string PlannedName => $"IMG_{SequenceNumber:D4}{Extension}";

    public string BadgeText => SequenceNumber.ToString("0000");

    public bool NeedsRename =>
        !string.Equals(FileName, PlannedName, StringComparison.OrdinalIgnoreCase);

    public void LoadThumbnail()
        => Thumbnail = ThumbnailLoader.TryLoad(FilePath, decodePixelWidth: 280);

    public void ApplyRenamedPath(string newPath) => FilePath = newPath;
}

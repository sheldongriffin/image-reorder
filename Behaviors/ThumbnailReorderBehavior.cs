using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ImageReorder.Services;
using ImageReorder.ViewModels;

namespace ImageReorder.Behaviors;

internal static class ThumbnailReorderBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(ThumbnailReorderBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    private static Point _dragStart;
    private static bool _readyToDrag;
    private static bool _isMoving;

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ItemsControl itemsControl)
        {
            return;
        }

        itemsControl.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        itemsControl.PreviewMouseMove -= OnPreviewMouseMove;
        itemsControl.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
        itemsControl.LostMouseCapture -= OnLostMouseCapture;

        if (e.NewValue is true)
        {
            itemsControl.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            itemsControl.PreviewMouseMove += OnPreviewMouseMove;
            itemsControl.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            itemsControl.LostMouseCapture += OnLostMouseCapture;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ItemsControl itemsControl)
        {
            return;
        }

        Guard(itemsControl, "button down", () =>
        {
            if (ItemTreeWalker.FindDataContext<ImageEntry>(e.OriginalSource) is not { } entry)
            {
                return;
            }

            _dragStart = e.GetPosition(itemsControl);
            _readyToDrag = true;
            AppLog.Trace($"Arming drag on {entry.FileName} at {_dragStart}.");
        });
    }

    private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not ItemsControl { DataContext: MainViewModel viewModel } itemsControl)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed || _isMoving)
        {
            return;
        }

        Guard(itemsControl, "mouse move", () =>
        {
            var position = e.GetPosition(itemsControl);

            if (_readyToDrag && viewModel.DraggingItem is null)
            {
                if (Math.Abs(position.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
                    Math.Abs(position.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
                {
                    return;
                }

                if (ItemTreeWalker.FindDataContext<ImageEntry>(e.OriginalSource) is not { } entry)
                {
                    return;
                }

                _readyToDrag = false;
                viewModel.BeginDrag(entry);
                itemsControl.CaptureMouse();
                itemsControl.Cursor = Cursors.SizeAll;
                AppLog.Info($"Drag started: {entry.FileName} from position {entry.SequenceNumber}.");
            }

            if (viewModel.DraggingItem is not { } dragging)
            {
                return;
            }

            var target = FindEntryAt(itemsControl, position);
            if (target is null || ReferenceEquals(target, dragging))
            {
                return;
            }

            var targetIndex = viewModel.Images.IndexOf(target);
            AppLog.Trace($"Hover target {target.FileName} (index {targetIndex}) while dragging {dragging.FileName}.");

            _isMoving = true;
            try
            {
                viewModel.MoveItem(dragging, targetIndex);
            }
            finally
            {
                _isMoving = false;
            }
        });
    }

    private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        => CompleteDrag(sender as ItemsControl);

    private static void OnLostMouseCapture(object sender, MouseEventArgs e)
        => CompleteDrag(sender as ItemsControl);

    private static void CompleteDrag(ItemsControl? itemsControl)
    {
        _readyToDrag = false;

        if (itemsControl?.DataContext is not MainViewModel viewModel || viewModel.DraggingItem is null)
        {
            return;
        }

        Guard(itemsControl, "drag completion", () =>
        {
            AppLog.Info($"Drag finished: {viewModel.DraggingItem?.FileName} at position {viewModel.DraggingItem?.SequenceNumber}.");
            viewModel.EndDrag();
            itemsControl.ReleaseMouseCapture();
            itemsControl.Cursor = Cursors.Arrow;
        });
    }

    private static ImageEntry? FindEntryAt(ItemsControl itemsControl, Point position)
        => ItemTreeWalker.FindDataContext<ImageEntry>(itemsControl.InputHitTest(position));

    private static void Guard(ItemsControl itemsControl, string operation, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            AppLog.Error($"Thumbnail reorder failed during {operation}.", ex);
            AbortDrag(itemsControl);
            throw;
        }
    }

    private static void AbortDrag(ItemsControl itemsControl)
    {
        try
        {
            _readyToDrag = false;
            _isMoving = false;

            if (itemsControl.DataContext is MainViewModel viewModel)
            {
                viewModel.EndDrag();
            }

            itemsControl.ReleaseMouseCapture();
            itemsControl.Cursor = Cursors.Arrow;
        }
        catch (Exception ex)
        {
            AppLog.Warn("Could not reset drag state after a failure.", ex);
        }
    }
}

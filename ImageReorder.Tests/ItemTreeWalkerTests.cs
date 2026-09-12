using System.Threading;
using System.Windows.Controls;
using System.Windows.Documents;
using ImageReorder.Behaviors;
using ImageReorder.ViewModels;
using Xunit;

namespace ImageReorder.Tests;

public sealed class ItemTreeWalkerTests
{
    [Fact]
    public void FindDataContext_FromContentElement_ReachesItemDataContext()
    {
        RunOnStaThread(() =>
        {
            var entry = new ImageEntry(@"C:\images\IMG_0001.jpg");
            var run = new Run("Will be IMG_0001.jpg");
            var textBlock = new TextBlock(run) { DataContext = entry };
            var card = new Border { Child = textBlock, DataContext = entry };

            Assert.NotNull(card);
            Assert.Same(entry, ItemTreeWalker.FindDataContext<ImageEntry>(run));
        });
    }

    [Fact]
    public void FindDataContext_FromNestedVisual_ReachesItemDataContext()
    {
        RunOnStaThread(() =>
        {
            var entry = new ImageEntry(@"C:\images\IMG_0002.jpg");
            var image = new Image();
            var inner = new Grid();
            inner.Children.Add(image);
            _ = new Border { Child = inner, DataContext = entry };

            Assert.Same(entry, ItemTreeWalker.FindDataContext<ImageEntry>(image));
        });
    }

    [Fact]
    public void FindDataContext_WithUnrelatedSource_ReturnsNull()
    {
        RunOnStaThread(() => Assert.Null(ItemTreeWalker.FindDataContext<ImageEntry>(new Button())));
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw new Xunit.Sdk.XunitException($"STA test body failed: {failure}");
        }
    }
}

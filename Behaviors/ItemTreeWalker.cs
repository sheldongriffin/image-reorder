using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ImageReorder.Behaviors;

internal static class ItemTreeWalker
{
    private const int MaxDepth = 256;

    /// <summary>
    /// Walks up from a hit-tested object to the nearest data context of type <typeparamref name="T"/>.
    /// Mouse events can originate from content elements such as <see cref="System.Windows.Documents.Run"/>,
    /// which are not visuals, so visual and logical parents both have to be followed.
    /// </summary>
    public static T? FindDataContext<T>(object? source) where T : class
    {
        var current = source as DependencyObject;

        for (var depth = 0; current is not null && depth < MaxDepth; depth++)
        {
            if (GetDataContext(current) is T match)
            {
                return match;
            }

            current = GetParent(current);
        }

        return null;
    }

    private static object? GetDataContext(DependencyObject element) => element switch
    {
        FrameworkElement frameworkElement => frameworkElement.DataContext,
        FrameworkContentElement contentElement => contentElement.DataContext,
        _ => null
    };

    private static DependencyObject? GetParent(DependencyObject element)
    {
        if (element is Visual or Visual3D && VisualTreeHelper.GetParent(element) is { } visualParent)
        {
            return visualParent;
        }

        if (element is ContentElement content && ContentOperations.GetParent(content) is { } contentParent)
        {
            return contentParent;
        }

        return LogicalTreeHelper.GetParent(element);
    }
}

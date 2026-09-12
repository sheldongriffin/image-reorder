using System.Text.RegularExpressions;

namespace ImageReorder.Services;

internal static partial class NaturalSort
{
    public static int CompareFileNames(string? left, string? right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left is null)
        {
            return -1;
        }

        if (right is null)
        {
            return 1;
        }

        var leftParts = ChunkRegex().Matches(left);
        var rightParts = ChunkRegex().Matches(right);
        var count = Math.Min(leftParts.Count, rightParts.Count);

        for (var i = 0; i < count; i++)
        {
            var a = leftParts[i].Value;
            var b = rightParts[i].Value;
            var comparison = int.TryParse(a, out var aNumber) && int.TryParse(b, out var bNumber)
                ? aNumber.CompareTo(bNumber)
                : string.Compare(a, b, StringComparison.OrdinalIgnoreCase);

            if (comparison != 0)
            {
                return comparison;
            }
        }

        return leftParts.Count.CompareTo(rightParts.Count);
    }

    [GeneratedRegex(@"\d+|\D+", RegexOptions.CultureInvariant)]
    private static partial Regex ChunkRegex();
}

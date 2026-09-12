using System.IO;

namespace ImageReorder.Services;

internal sealed record RenameRequest(string SourcePath, string DestinationFileName);

internal static class CollisionSafeRenamer
{
    private const string TempPrefix = ".imgreorder-";

    public static IReadOnlyList<(string FromName, string ToName)> Plan(IReadOnlyList<RenameRequest> requests)
    {
        var plan = new List<(string FromName, string ToName)>();
        foreach (var request in requests)
        {
            var currentName = Path.GetFileName(request.SourcePath);
            if (string.Equals(currentName, request.DestinationFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            plan.Add((currentName, request.DestinationFileName));
        }

        return plan;
    }

    public static IReadOnlyList<string> Execute(IReadOnlyList<RenameRequest> requests)
    {
        if (requests.Count == 0)
        {
            return [];
        }

        var directory = Path.GetDirectoryName(requests[0].SourcePath)
            ?? throw new InvalidOperationException("Images must live in a folder.");

        Validate(directory, requests);

        var staged = new List<(string TempPath, string FinalPath, string OriginalPath)>();
        var completed = new List<string>();

        try
        {
            foreach (var request in requests)
            {
                var currentName = Path.GetFileName(request.SourcePath);
                if (string.Equals(currentName, request.DestinationFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var extension = Path.GetExtension(request.SourcePath);
                var tempPath = Path.Combine(directory, $"{TempPrefix}{Guid.NewGuid():N}{extension}");
                File.Move(request.SourcePath, tempPath);
                staged.Add((tempPath, Path.Combine(directory, request.DestinationFileName), request.SourcePath));
            }

            foreach (var (tempPath, finalPath, _) in staged)
            {
                File.Move(tempPath, finalPath);
                completed.Add(finalPath);
            }

            return completed;
        }
        catch
        {
            foreach (var (tempPath, _, originalPath) in staged)
            {
                if (File.Exists(tempPath) && !File.Exists(originalPath))
                {
                    File.Move(tempPath, originalPath);
                }
            }

            throw;
        }
    }

    private static void Validate(string directory, IReadOnlyList<RenameRequest> requests)
    {
        var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var managedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var request in requests)
        {
            var sourceDirectory = Path.GetDirectoryName(request.SourcePath);
            if (!string.Equals(sourceDirectory, directory, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("All images must be in the same folder.");
            }

            if (!File.Exists(request.SourcePath))
            {
                throw new FileNotFoundException("An image is missing from the folder.", request.SourcePath);
            }

            if (!destinations.Add(request.DestinationFileName))
            {
                throw new InvalidOperationException($"Two images would both be named {request.DestinationFileName}.");
            }

            managedNames.Add(Path.GetFileName(request.SourcePath));
        }

        foreach (var request in requests)
        {
            var destinationPath = Path.Combine(directory, request.DestinationFileName);
            if (!File.Exists(destinationPath))
            {
                continue;
            }

            var occupyingName = Path.GetFileName(destinationPath);
            if (!managedNames.Contains(occupyingName))
            {
                throw new IOException(
                    $"Cannot rename to {request.DestinationFileName} because that name is already used by a file that is not part of this reorder.");
            }
        }
    }
}

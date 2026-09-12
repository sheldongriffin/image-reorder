using System.IO;
using ImageReorder.Services;
using Xunit;

namespace ImageReorder.Tests;

public sealed class CollisionSafeRenamerTests
{
    [Fact]
    public void Swap_RenamesWithoutColliding()
    {
        var folder = CreateTempFolder();
        try
        {
            var first = Path.Combine(folder, "IMG_0001.txt");
            var second = Path.Combine(folder, "IMG_0002.txt");
            File.WriteAllText(first, "one");
            File.WriteAllText(second, "two");

            CollisionSafeRenamer.Execute(
            [
                new RenameRequest(first, "IMG_0002.txt"),
                new RenameRequest(second, "IMG_0001.txt")
            ]);

            Assert.Equal("one", File.ReadAllText(Path.Combine(folder, "IMG_0002.txt")));
            Assert.Equal("two", File.ReadAllText(Path.Combine(folder, "IMG_0001.txt")));
            Assert.Empty(Directory.GetFiles(folder, ".imgreorder-*"));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Rotation_UsesTemporaryNames()
    {
        var folder = CreateTempFolder();
        try
        {
            var a = Path.Combine(folder, "IMG_0001.txt");
            var b = Path.Combine(folder, "IMG_0002.txt");
            var c = Path.Combine(folder, "IMG_0003.txt");
            File.WriteAllText(a, "a");
            File.WriteAllText(b, "b");
            File.WriteAllText(c, "c");

            CollisionSafeRenamer.Execute(
            [
                new RenameRequest(a, "IMG_0002.txt"),
                new RenameRequest(b, "IMG_0003.txt"),
                new RenameRequest(c, "IMG_0001.txt")
            ]);

            Assert.Equal("c", File.ReadAllText(Path.Combine(folder, "IMG_0001.txt")));
            Assert.Equal("a", File.ReadAllText(Path.Combine(folder, "IMG_0002.txt")));
            Assert.Equal("b", File.ReadAllText(Path.Combine(folder, "IMG_0003.txt")));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void AlreadyNamed_LeavesFilesUntouched()
    {
        var folder = CreateTempFolder();
        try
        {
            var first = Path.Combine(folder, "IMG_0001.txt");
            File.WriteAllText(first, "one");

            CollisionSafeRenamer.Execute([new RenameRequest(first, "IMG_0001.txt")]);

            Assert.True(File.Exists(first));
            Assert.Equal("one", File.ReadAllText(first));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static string CreateTempFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "image-reorder-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }
}

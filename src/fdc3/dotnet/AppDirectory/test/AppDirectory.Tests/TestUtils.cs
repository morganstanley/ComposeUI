using System.Text;
using Testably.Abstractions.Testing;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

internal static class TestUtils
{
    public static MockFileSystem SetUpFileSystemWithSingleFile(string path, string contents)
    {
        var fileSystem = new MockFileSystem();
        fileSystem
            .Initialize()
            .WithFile(path)
            .Which(f => f.HasBytesContent(Encoding.UTF8.GetBytes(contents)));

        return fileSystem;
    }
}
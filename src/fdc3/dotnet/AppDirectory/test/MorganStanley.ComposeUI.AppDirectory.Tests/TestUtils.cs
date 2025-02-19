/*
* Morgan Stanley makes this available to you under the Apache License,
* Version 2.0 (the "License"). You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0.
*
* See the NOTICE file distributed with this work for additional information
* regarding copyright ownership. Unless required by applicable law or agreed
* to in writing, software distributed under the License is distributed on an
* "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
* or implied. See the License for the specific language governing permissions
* and limitations under the License.
*/

using System.IO.Abstractions;
using System.Text;
using MorganStanley.ComposeUI.Fdc3.AppDirectory.TestUtilities;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

internal static class TestUtils
{
    public static IFileSystem SetUpFileSystemWithSingleFile(string path, string contents)
    {
        var fileSystemWatcherMock = GetFileSystemWatcher(path);

        var fileSystem = new Mock<IFileSystem>();
        fileSystem
            .Setup(_ => _.File)
            .Returns(GetFile(path, contents, fileSystemWatcherMock));

        fileSystem
            .Setup(_ => _.FileSystemWatcher)
            .Returns(GetFileSystemWatcherFactory(fileSystemWatcherMock));

        fileSystem
            .Setup(_ => _.Path)
            .Returns(GetPath(path));

        return fileSystem.Object;
    }

    private static IPath GetPath(string path)
    {
        var mockPath = new Mock<IPath>();
        mockPath
            .Setup(_ => _.GetDirectoryName(It.IsAny<string>()))
            .Returns(Directory.GetDirectoryRoot(path));

        mockPath
            .Setup(_ => _.GetFileName(It.IsAny<string>()))
            .Returns(path.Trim('/'));

        return mockPath.Object;
    }

    private static IFileSystemWatcherFactory GetFileSystemWatcherFactory(IFileSystemWatcher fileSystemWatcherMock)
    {
        var fileSystemWatcherFactory = new Mock<IFileSystemWatcherFactory>();
        fileSystemWatcherFactory
            .Setup(_ => _.New(It.IsAny<string>()))
            .Returns(fileSystemWatcherMock);

        fileSystemWatcherFactory
            .Setup(_ => _.New(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(fileSystemWatcherMock);

        fileSystemWatcherFactory
            .Setup(_ => _.New())
            .Returns(fileSystemWatcherMock);

        return fileSystemWatcherFactory.Object;
    }

    private static IFileSystemWatcher GetFileSystemWatcher(string path)
    {
        var fileSystemWatcher = new Mock<IFileSystemWatcher>();
        FileSystemEventHandler? onChangedHandler = null;

        fileSystemWatcher
            .SetupAdd(_ => _.Changed += It.IsAny<FileSystemEventHandler>())
            .Callback<FileSystemEventHandler>(handler => onChangedHandler += handler);

        fileSystemWatcher
            .SetupRemove(_ => _.Changed -= It.IsAny<FileSystemEventHandler>())
            .Callback<FileSystemEventHandler>(handler => onChangedHandler -= handler);

        fileSystemWatcher
            .SetupProperty(fsw => fsw.EnableRaisingEvents, true);

        fileSystemWatcher
            .Setup(_ => _.Dispose())
            .Callback(() => onChangedHandler = null);

        fileSystemWatcher.As<IFileSystemWatcherMockHelper>()
            .Setup(_ => _.TriggerChangedEvent())
            .Callback(() =>
            {
                onChangedHandler?.Invoke(
                    fileSystemWatcher.Object, 
                    new FileSystemEventArgs(
                        WatcherChangeTypes.Changed,
                        Path.GetDirectoryName(path) ?? "",
                        Path.GetFileName(path)
                ));
            });

        return fileSystemWatcher.Object;
    }

    private static IFile GetFile(string path, string contents, IFileSystemWatcher? fileSystemWatcher = null)
    {
        var file = new Mock<IFile>();

        var fileContents = new Dictionary<string, string>
        {
            [path] = contents
        };

        file
            .Setup(_ => _.Exists(It.IsAny<string>()))
            .Returns((string path) => fileContents.ContainsKey(path));

        file
            .Setup(_ => _.OpenRead(It.IsAny<string>()))
            .Returns((string path) =>
            {
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContents[path]));
                return GetFileSystemStreamValue(stream, path, false);
            });

        file
            .Setup(_ => _.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string path, string content, CancellationToken _) =>
            {
                fileContents[path] = content;

                if (fileSystemWatcher is IFileSystemWatcherMockHelper watcherHelper)
                {
                    watcherHelper.TriggerChangedEvent();
                }

                return Task.CompletedTask;
            });

        file
            .Setup(_ => _.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Encoding>(), It.IsAny<CancellationToken>()))
            .Returns((string path, string content, Encoding _, CancellationToken _) =>
            {
                fileContents[path] = content;

                if (fileSystemWatcher is IFileSystemWatcherMockHelper watcherHelper)
                {
                    watcherHelper.TriggerChangedEvent();
                }

                return Task.CompletedTask;
            });

        return file.Object;
    }

    private static FileSystemStream GetFileSystemStreamValue(Stream content, string path, bool isAsync)
    {
        return new MockFileSystemStream(content, path, isAsync);
    }

    private class MockFileSystemStream(Stream stream, string path, bool isAsync) 
        : FileSystemStream(stream, path, isAsync)
    {
    }
}
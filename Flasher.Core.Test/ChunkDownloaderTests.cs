using Flasher.Core.Download;
using Flasher.Core.Sony;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flasher.Core.Test;

[TestClass]
public sealed class ChunkDownloaderTests
{
    [TestMethod]
    public async Task DownloadsChunkAndVerifiesEtag()
    {
        var data = Enumerable.Range(1, 100).Select(value => (byte)value).ToArray();
        var md5 = TestHttpMessageHandler.Md5(data);
        var client = new HttpClient(new TestHttpMessageHandler(_ =>
            TestHttpMessageHandler.Full(data, md5)));
        var path = TempFile();

        try
        {
            var downloader = new ChunkDownloader(client, new DownloadOptions());
            var result = await downloader.DownloadAsync(Chunk(data.Length), path, null, CancellationToken.None);

            Assert.AreEqual(md5, result);
            CollectionAssert.AreEqual(data, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task ResumesFromPartialFile()
    {
        var data = Enumerable.Range(1, 100).Select(value => (byte)value).ToArray();
        var md5 = TestHttpMessageHandler.Md5(data);
        var path = TempFile();
        await File.WriteAllBytesAsync(path, data[..20]);

        try
        {
            var requestCount = 0;
            var client = new HttpClient(new TestHttpMessageHandler(request =>
            {
                requestCount++;
                var range = request.Headers.Range;
                Assert.IsNotNull(range);
                Assert.AreEqual(20L, range.Ranges.First().From);
                return TestHttpMessageHandler.Partial(data[20..], 20, data.Length - 1, data.Length, md5);
            }));

            var downloader = new ChunkDownloader(client, new DownloadOptions());
            var result = await downloader.DownloadAsync(Chunk(data.Length), path, null, CancellationToken.None);

            Assert.AreEqual(md5, result);
            Assert.AreEqual(1, requestCount);
            CollectionAssert.AreEqual(data, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task RejectsShortChunk()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        var client = new HttpClient(new TestHttpMessageHandler(_ =>
            TestHttpMessageHandler.Full(data, TestHttpMessageHandler.Md5(data))));
        var path = TempFile();

        try
        {
            var downloader = new ChunkDownloader(client, new DownloadOptions());
            await Assert.ThrowsExactlyAsync<InvalidDataException>(
                () => downloader.DownloadAsync(Chunk(5), path, null, CancellationToken.None));
            Assert.IsFalse(File.Exists(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task RejectsWrongEtag()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        var client = new HttpClient(new TestHttpMessageHandler(_ =>
            TestHttpMessageHandler.Full(data, new string('0', 32))));
        var path = TempFile();

        try
        {
            var downloader = new ChunkDownloader(client, new DownloadOptions());
            await Assert.ThrowsExactlyAsync<InvalidDataException>(
                () => downloader.DownloadAsync(Chunk(4), path, null, CancellationToken.None));
            Assert.IsFalse(File.Exists(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static SonyFileChunk Chunk(long size)
    {
        return new SonyFileChunk
        {
            Number = 0,
            Size = size,
            DownloadLink = new SonyLink(
                "download",
                "https://app.swup.update.sony.net/filestorage/1/token_part0")
        };
    }

    private static string TempFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"flasher-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "00000.part");
    }
}

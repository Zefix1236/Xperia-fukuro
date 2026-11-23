using Flasher.Core.Download;
using Flasher.Core.Sony;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flasher.Core.Test;

[TestClass]
public sealed class DownloadManagerTests
{
    [TestMethod]
    public async Task DownloadsAssemblesAndDeletesChunks()
    {
        var first = new byte[] { 1, 2, 3, 4 };
        var second = new byte[] { 5, 6, 7 };
        var client = new HttpClient(new TestHttpMessageHandler(request =>
        {
            var data = request.RequestUri!.AbsolutePath.EndsWith("_part0", StringComparison.Ordinal)
                ? first
                : second;
            var md5 = TestHttpMessageHandler.Md5(data);
            var range = request.Headers.Range;
            if (range is null)
                return TestHttpMessageHandler.Full(data, md5);

            var from = (int)range.Ranges.First().From!.Value;
            return TestHttpMessageHandler.Partial(
                data[from..],
                from,
                data.Length - 1,
                data.Length,
                md5);
        }));

        var directory = Path.Combine(Path.GetTempPath(), $"flasher-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var file = new SonyFileResource
            {
                ContentId = "1",
                FileKey = "APP_SW",
                Name = "firmware.zip",
                PlainSize = 7
            };
            var manifest = new SonyFileManifest
            {
                ContentId = "1",
                Chunks =
                [
                    new SonyFileChunk
                    {
                        Number = 0,
                        Size = 4,
                        DownloadLink = new SonyLink(
                            "download",
                            "https://app.swup.update.sony.net/filestorage/1/token_part0")
                    },
                    new SonyFileChunk
                    {
                        Number = 1,
                        Size = 3,
                        DownloadLink = new SonyLink(
                            "download",
                            "https://app.swup.update.sony.net/filestorage/1/token_part1")
                    }
                ]
            };

            using var manager = new DownloadManager(
                client,
                new DownloadOptions { DiskSpaceMarginBytes = 0 });
            var result = await manager.DownloadAsync(
                file,
                manifest,
                directory,
                cancellationToken: CancellationToken.None);

            Assert.AreEqual("APP_SW", result.FileKey);
            Assert.AreEqual(7, result.Size);
            Assert.IsNotNull(result.Md5);
            CollectionAssert.AreEqual(
                new byte[] { 1, 2, 3, 4, 5, 6, 7 },
                await File.ReadAllBytesAsync(result.FilePath));
            Assert.IsFalse(Directory.Exists(Path.Combine(directory, ".chunks")));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}

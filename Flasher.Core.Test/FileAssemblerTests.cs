using Flasher.Core.Download;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flasher.Core.Test;

[TestClass]
public sealed class FileAssemblerTests
{
    [TestMethod]
    public async Task AssemblesChunksInOrder()
    {
        var directory = CreateTempDirectory();
        try
        {
            var chunks = new[]
            {
                Write(directory, "00000.part", [1, 2]),
                Write(directory, "00001.part", [3, 4, 5]),
                Write(directory, "00002.part", [6])
            };
            var output = Path.Combine(directory, "firmware.zip");

            await FileAssembler.AssembleAsync(chunks, output, 6, CancellationToken.None);

            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6 }, await File.ReadAllBytesAsync(output));
            Assert.AreEqual(6, new FileInfo(output).Length);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task RejectsWrongTotalSize()
    {
        var directory = CreateTempDirectory();
        try
        {
            var chunks = new[] { Write(directory, "00000.part", [1, 2, 3]) };
            await Assert.ThrowsExactlyAsync<InvalidDataException>(
                () => FileAssembler.AssembleAsync(
                    chunks,
                    Path.Combine(directory, "firmware.zip"),
                    4,
                    CancellationToken.None));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string Write(string directory, string name, byte[] content)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllBytes(path, content);
        return path;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"flasher-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}

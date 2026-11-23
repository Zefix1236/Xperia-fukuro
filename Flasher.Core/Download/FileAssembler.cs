namespace Flasher.Core.Download;

public static class FileAssembler
{
    private const int BufferSize = 1024 * 1024;

    /// <summary>按分块顺序合并为最终文件；先写临时文件再原子改名。</summary>
    public static async Task AssembleAsync(
        IReadOnlyList<string> chunkPaths,
        string outputPath,
        long expectedSize,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chunkPaths);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (chunkPaths.Count == 0)
            throw new ArgumentException("没有可合并的分块", nameof(chunkPaths));

        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath))!;
        Directory.CreateDirectory(directory);
        var tempPath = Path.Combine(directory, $"{Path.GetFileName(outputPath)}.assembling");

        try
        {
            long total = 0;
            await using (var output = new FileStream(
                             tempPath,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             BufferSize,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                foreach (var chunkPath in chunkPaths)
                {
                    await using var input = new FileStream(
                        chunkPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        BufferSize,
                        FileOptions.Asynchronous | FileOptions.SequentialScan);
                    await input.CopyToAsync(output, BufferSize, cancellationToken).ConfigureAwait(false);
                    total += input.Length;
                }

                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                if (output.Length != expectedSize)
                    throw new InvalidDataException(
                        $"合并后大小不符：{output.Length} != {expectedSize}");
            }

            if (total != expectedSize)
                throw new InvalidDataException($"分块总大小不符：{total} != {expectedSize}");

            File.Move(tempPath, outputPath, true);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    public static async Task<string> ComputeMd5Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await System.Security.Cryptography.MD5
            .HashDataAsync(stream, cancellationToken)
            .ConfigureAwait(false);
        return Convert.ToHexStringLower(hash);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // 清理失败不覆盖原始异常。
        }
    }
}

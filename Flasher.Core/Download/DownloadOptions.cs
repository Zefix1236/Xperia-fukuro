namespace Flasher.Core.Download;

public sealed record DownloadOptions
{
    /// <summary>单个固件内并发下载的分块数。</summary>
    public int MaxConcurrency { get; init; } = 4;

    /// <summary>单个分块的最大重试次数。</summary>
    public int MaxRetries { get; init; } = 3;

    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>合并完成后删除临时分块。</summary>
    public bool DeleteChunksAfterAssembly { get; init; } = true;

    /// <summary>合并后计算整个文件的 MD5。</summary>
    public bool ComputeFileMd5 { get; init; } = true;

    /// <summary>磁盘预检时额外预留的空间。</summary>
    public long DiskSpaceMarginBytes { get; init; } = 128L * 1024 * 1024;
}

public sealed record DownloadProgress(
    string FileKey,
    long BytesDownloaded,
    long TotalBytes,
    int CompletedChunks,
    int TotalChunks,
    double BytesPerSecond)
{
    public double Fraction => TotalBytes <= 0
        ? 0
        : Math.Clamp((double)BytesDownloaded / TotalBytes, 0, 1);
}

public sealed record DownloadResult(
    string FileKey,
    string FilePath,
    long Size,
    string? Md5);

public sealed class InsufficientDiskSpaceException(long requiredBytes, long availableBytes)
    : Exception($"磁盘空间不足：需要 {Format(requiredBytes)}，可用 {Format(availableBytes)}")
{
    public long RequiredBytes { get; } = requiredBytes;

    public long AvailableBytes { get; } = availableBytes;

    private static string Format(long bytes) => $"{bytes / 1024d / 1024d / 1024d:F2} GiB";
}

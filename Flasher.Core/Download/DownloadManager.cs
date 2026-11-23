using System.Diagnostics;
using System.Net;
using Flasher.Core.Sony;

namespace Flasher.Core.Download;

/// <summary>单个固件文件的下载：分块并发、Range 续传、ETag=MD5 校验、合并与清理。</summary>
public sealed class DownloadManager : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly DownloadOptions _options;

    public DownloadManager(HttpClient? httpClient = null, DownloadOptions? options = null)
    {
        _ownsHttpClient = httpClient is null;
        _http = httpClient ?? CreateDefaultClient();
        _options = options ?? new DownloadOptions();
    }

    public DownloadOptions Options => _options;

    public async Task<DownloadResult> DownloadAsync(
        SonyFileResource file,
        SonyFileManifest manifest,
        string outputDirectory,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        Validate(file, manifest);
        Directory.CreateDirectory(outputDirectory);

        var outputPath = Path.Combine(outputDirectory, SafeFileName(file.Name, file.FileKey));
        var chunkDirectory = Path.Combine(
            outputDirectory,
            ".chunks",
            SafeFileName(file.ContentId ?? file.FileKey, file.FileKey));
        Directory.CreateDirectory(chunkDirectory);

        var chunkPaths = manifest.Chunks
            .Select(chunk => Path.Combine(chunkDirectory, $"{chunk.Number:D5}.part"))
            .ToArray();

        var initialBytes = 0L;
        var pendingBytes = 0L;
        for (var index = 0; index < manifest.Chunks.Count; index++)
        {
            var chunk = manifest.Chunks[index];
            var path = chunkPaths[index];
            var existing = File.Exists(path) ? new FileInfo(path).Length : 0;
            if (existing > chunk.Size)
            {
                File.Delete(path);
                existing = 0;
            }

            initialBytes += existing;
            pendingBytes += chunk.Size - existing;
        }

        var requiredBytes = pendingBytes + file.PlainSize + _options.DiskSpaceMarginBytes;
        var availableBytes = GetAvailableFreeSpace(outputDirectory);
        if (availableBytes < requiredBytes)
            throw new InsufficientDiskSpaceException(requiredBytes, availableBytes);

        var state = new ProgressState(file.FileKey, file.PlainSize, manifest.Chunks.Count, initialBytes, progress);
        state.ReportNow();

        await Parallel.ForEachAsync(
                manifest.Chunks,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Max(1, _options.MaxConcurrency),
                    CancellationToken = cancellationToken
                },
                async (chunk, token) =>
                {
                    var path = chunkPaths[chunk.Number];
                    var existing = File.Exists(path) ? new FileInfo(path).Length : 0;
                    if (existing == chunk.Size)
                    {
                        state.CompleteChunk();
                        return;
                    }

                    var downloader = new ChunkDownloader(_http, _options);
                    await downloader.DownloadAsync(
                            chunk,
                            path,
                            new SyncProgress<long>(state.AddBytes),
                            token)
                        .ConfigureAwait(false);
                    state.CompleteChunk();
                })
            .ConfigureAwait(false);

        state.ReportNow(force: true);
        await FileAssembler
            .AssembleAsync(chunkPaths, outputPath, file.PlainSize, cancellationToken)
            .ConfigureAwait(false);

        var md5 = _options.ComputeFileMd5
            ? await FileAssembler.ComputeMd5Async(outputPath, cancellationToken).ConfigureAwait(false)
            : null;

        if (_options.DeleteChunksAfterAssembly)
        {
            TryDeleteDirectory(chunkDirectory);
            TryDeleteEmptyDirectory(Path.Combine(outputDirectory, ".chunks"));
        }

        return new DownloadResult(file.FileKey, outputPath, file.PlainSize, md5);
    }

    private static void Validate(SonyFileResource file, SonyFileManifest manifest)
    {
        if (manifest.Chunks.Count == 0)
            throw new InvalidDataException("Manifest 没有分块");
        if (manifest.TotalSize != file.PlainSize)
            throw new InvalidDataException(
                $"Manifest 总大小 {manifest.TotalSize} 与文件大小 {file.PlainSize} 不一致");
        if (manifest.ContentId is not null
            && file.ContentId is not null
            && !string.Equals(manifest.ContentId, file.ContentId, StringComparison.Ordinal))
            throw new InvalidDataException("Manifest 与文件资源的 content-id 不一致");

        var expectedNumbers = Enumerable.Range(0, manifest.Chunks.Count);
        if (!manifest.Chunks.Select(chunk => chunk.Number).SequenceEqual(expectedNumbers))
            throw new InvalidDataException("Manifest 分块编号不连续");
        if (manifest.Chunks.Any(chunk => chunk.Size < 0))
            throw new InvalidDataException("Manifest 存在负的分块大小");
    }

    private static string SafeFileName(string fileName, string fallback)
    {
        var name = Path.GetFileName(fileName);
        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');
        name = name.Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(name))
            name = fallback;
        if (name.Length > 180)
            name = name[..180];
        return name;
    }

    private static long GetAvailableFreeSpace(string path)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(path));
            return string.IsNullOrEmpty(root) ? long.MaxValue : new DriveInfo(root).AvailableFreeSpace;
        }
        catch
        {
            return long.MaxValue;
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch
        {
            // 清理失败不影响下载结果。
        }
    }

    private static void TryDeleteEmptyDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
                Directory.Delete(path);
        }
        catch
        {
            // 清理失败不影响下载结果。
        }
    }

    private static HttpClient CreateDefaultClient()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(20),
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        };
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(30)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("XperiaFukuro/0.1");
        return client;
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _http.Dispose();
    }

    private sealed class SyncProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value)
        {
            handler(value);
        }
    }

    private sealed class ProgressState(
        string fileKey,
        long totalBytes,
        int totalChunks,
        long initialBytes,
        IProgress<DownloadProgress>? sink)
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private readonly Lock _gate = new();
        private long _bytes = initialBytes;
        private int _completedChunks;
        private long _lastReportMs = -1000;

        public void AddBytes(long delta)
        {
            var value = Interlocked.Add(ref _bytes, delta);
            if (value < 0)
                Interlocked.Exchange(ref _bytes, 0);
            else if (value > totalBytes)
                Interlocked.Exchange(ref _bytes, totalBytes);
            ReportNow();
        }

        public void CompleteChunk()
        {
            Interlocked.Increment(ref _completedChunks);
            ReportNow(force: true);
        }

        public void ReportNow(bool force = false)
        {
            if (sink is null)
                return;
            lock (_gate)
            {
                var elapsedMs = _watch.ElapsedMilliseconds;
                var bytes = Math.Clamp(Interlocked.Read(ref _bytes), 0, totalBytes);
                var chunks = Volatile.Read(ref _completedChunks);
                if (!force && elapsedMs - _lastReportMs < 100 && bytes < totalBytes)
                    return;
                _lastReportMs = elapsedMs;
                var speed = _watch.Elapsed.TotalSeconds <= 0
                    ? 0
                    : bytes / _watch.Elapsed.TotalSeconds;
                sink.Report(
                    new DownloadProgress(fileKey, bytes, totalBytes, chunks, totalChunks, speed));
            }
        }
    }
}

using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;

namespace Flasher.Core.Download;

/// <summary>下载单个分块；支持 Range 续传与 ETag=MD5 校验。</summary>
public sealed class ChunkDownloader(HttpClient httpClient, DownloadOptions options)
{
    private const int BufferSize = 128 * 1024;

    public async Task<string?> DownloadAsync(
        Sony.SonyFileChunk chunk,
        string chunkPath,
        IProgress<long>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentException.ThrowIfNullOrWhiteSpace(chunkPath);

        Directory.CreateDirectory(Path.GetDirectoryName(chunkPath)!);

        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await DownloadOnceAsync(chunk, chunkPath, progress, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < options.MaxRetries && IsRetryable(ex, cancellationToken))
            {
                await Task.Delay(
                        TimeSpan.FromMilliseconds(
                            options.RetryDelay.TotalMilliseconds * Math.Pow(2, attempt)),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private async Task<string?> DownloadOnceAsync(
        Sony.SonyFileChunk chunk,
        string chunkPath,
        IProgress<long>? progress,
        CancellationToken cancellationToken)
    {
        var uri = ResolveUri(chunk.DownloadLink.Href);
        var existing = File.Exists(chunkPath) ? new FileInfo(chunkPath).Length : 0;
        if (existing == chunk.Size)
            return null;
        if (existing > chunk.Size)
        {
            File.Delete(chunkPath);
            progress?.Report(-existing);
            existing = 0;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (existing > 0)
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existing, null);

        using var response = await httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable && existing == chunk.Size)
            return null;
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"分块 {chunk.Number} 请求失败：{(int)response.StatusCode} {response.StatusCode}",
                null,
                response.StatusCode);

        var append = existing > 0 && response.StatusCode == HttpStatusCode.PartialContent;
        if (!append && existing > 0)
        {
            progress?.Report(-existing);
            existing = 0;
        }

        var mode = append ? FileMode.Append : FileMode.Create;
        var expectedLength = chunk.Size;
        long written = existing;
        await using (var file = new FileStream(
                         chunkPath,
                         mode,
                         FileAccess.Write,
                         FileShare.None,
                         BufferSize,
                         FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            var buffer = new byte[BufferSize];
            int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                written += read;
                progress?.Report(read);
                if (written > expectedLength)
                    throw new InvalidDataException(
                        $"分块 {chunk.Number} 超出预期大小：{written} > {expectedLength}");
            }
        }

        var actual = new FileInfo(chunkPath).Length;
        if (actual != expectedLength)
        {
            File.Delete(chunkPath);
            progress?.Report(-actual);
            throw new InvalidDataException(
                $"分块 {chunk.Number} 大小不符：{actual} != {expectedLength}");
        }

        var md5 = await ComputeMd5Async(chunkPath, cancellationToken).ConfigureAwait(false);
        var expectedMd5 = NormalizeEtag(response.Headers.ETag?.Tag);
        if (expectedMd5 is not null
            && !string.Equals(md5, expectedMd5, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(chunkPath);
            progress?.Report(-actual);
            throw new InvalidDataException($"分块 {chunk.Number} MD5 校验失败");
        }

        return md5;
    }

    private static Uri ResolveUri(string href)
    {
        if (!Uri.TryCreate(href, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("分块下载地址不是 HTTPS 绝对地址");
        return uri;
    }

    private static async Task<string> ComputeMd5Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await MD5.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexStringLower(hash);
    }

    private static string? NormalizeEtag(string? etag)
    {
        if (string.IsNullOrWhiteSpace(etag))
            return null;
        var value = etag.Trim();
        if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
            value = value[2..];
        value = value.Trim('"');
        return value.Length == 32
            && !value.Contains('-')
            && value.All(char.IsAsciiHexDigit)
                ? value.ToLowerInvariant()
                : null;
    }

    private static bool IsRetryable(Exception exception, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return false;
        return exception switch
        {
            HttpRequestException { StatusCode: null } => true,
            HttpRequestException { StatusCode: HttpStatusCode.RequestTimeout } => true,
            HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } => true,
            HttpRequestException { StatusCode: >= HttpStatusCode.InternalServerError } => true,
            TaskCanceledException => true,
            IOException => true,
            _ => false
        };
    }
}

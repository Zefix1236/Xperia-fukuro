using System.Net;
using System.Security.Cryptography;

namespace Flasher.Core.Test;

internal sealed class TestHttpMessageHandler(
    Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(responder(request));
    }

    public static HttpResponseMessage Full(byte[] content, string? etag = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(content)
        };
        if (etag is not null)
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue($"\"{etag}\"");
        return response;
    }

    public static HttpResponseMessage Partial(
        byte[] content,
        long start,
        long end,
        long total,
        string? etag = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.PartialContent)
        {
            Content = new ByteArrayContent(content)
        };
        response.Content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(
            start,
            end,
            total);
        if (etag is not null)
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue($"\"{etag}\"");
        return response;
    }

    public static string Md5(byte[] content) => Convert.ToHexStringLower(MD5.HashData(content));
}

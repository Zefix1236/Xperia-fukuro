using System.Net;
using System.Text.RegularExpressions;

namespace Flasher.Core.Sony;

/// <summary>日志/异常中使用的脱敏工具。下载 URL 的 query 与 filestorage token 都按敏感处理。</summary>
public static class SonySecretRedactor
{
    private static readonly Regex QuerySecretRegex = new(
        @"(?i)(password|MiscPassword|CeParam|MiscPackageId)=([^&""'<>\s]+)",
        RegexOptions.Compiled);

    private static readonly Regex StorageTokenRegex = new(
        @"(/filestorage/\d+/)[^/""'<>\s]+(_part\d+)",
        RegexOptions.Compiled);

    public static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        var result = QuerySecretRegex.Replace(text, "$1=REDACTED");
        return StorageTokenRegex.Replace(result, "$1REDACTED$2");
    }

    public static string RedactUri(Uri? uri)
    {
        return uri is null ? "" : Redact(uri.AbsoluteUri);
    }
}

public sealed class SonyApiException : Exception
{
    public SonyApiException(HttpStatusCode statusCode, Uri requestUri, string responseBody)
        : base($"Sony API 请求失败：{(int)statusCode} {statusCode}，{SonySecretRedactor.RedactUri(requestUri)}")
    {
        StatusCode = statusCode;
        RequestUri = requestUri;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }

    public Uri RequestUri { get; }

    public string ResponseBody { get; }
}

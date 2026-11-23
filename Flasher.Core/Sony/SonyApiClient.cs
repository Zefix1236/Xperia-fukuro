using System.Net;

namespace Flasher.Core.Sony;

/// <summary>Sony ESS 客户端：match → software-service → file manifest。只跟随服务端返回的 link。</summary>
public sealed class SonyApiClient : IDisposable
{
    public const string MatchEndpoint =
        "https://app.swup.update.sony.net/ess-distribution/public/api/device-service/match/v2";

    private const string ApiHost = "app.swup.update.sony.net";

    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;

    public SonyApiClient(HttpClient? httpClient = null)
    {
        _ownsHttpClient = httpClient is null;
        _http = httpClient ?? CreateDefaultClient();
    }

    public static Uri BuildMatchUri(SonyDeviceSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        selection.Validate();

        var query = string.Join(
            "&",
            new (string Key, string Value)[]
            {
                ("HwSetupKey", "Default"),
                ("HWVariantId", selection.HwVariantId),
                ("ModelObjectId", selection.ModelId),
                ("ProductObjectId", selection.ProductId),
                ("SecurityStateType", "COMMERCIAL"),
                ("SonyProductCode", selection.SonyProductCode)
            }.Select(pair => $"{pair.Key}={Uri.EscapeDataString(pair.Value)}"));

        return new Uri($"{MatchEndpoint}?{query}", UriKind.Absolute);
    }

    public async Task<SonyMatchResponse> MatchAsync(
        SonyDeviceSelection selection,
        CancellationToken cancellationToken = default)
    {
        var uri = BuildMatchUri(selection);
        var xml = await GetStringAsync(uri, cancellationToken).ConfigureAwait(false);
        return SonyXmlParser.ParseMatch(xml);
    }

    public async Task<SonySoftwareService> GetServiceAsync(
        SonyLink link,
        CancellationToken cancellationToken = default)
    {
        var uri = ResolveApiUri(link);
        var xml = await GetStringAsync(uri, cancellationToken).ConfigureAwait(false);
        return SonyXmlParser.ParseService(xml);
    }

    public async Task<SonyFileManifest> GetManifestAsync(
        SonyLink link,
        CancellationToken cancellationToken = default)
    {
        var uri = ResolveApiUri(link);
        var xml = await GetStringAsync(uri, cancellationToken).ConfigureAwait(false);
        return SonyXmlParser.ParseManifest(xml);
    }

    private async Task<string> GetStringAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var response = await _http
            .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new SonyApiException(response.StatusCode, uri, body);
        return body;
    }

    private static Uri ResolveApiUri(SonyLink link)
    {
        if (string.IsNullOrWhiteSpace(link.Href))
            throw new ArgumentException("link 地址为空", nameof(link));

        var uri = Uri.TryCreate(link.Href, UriKind.Absolute, out var absolute)
            ? absolute
            : new Uri(new Uri(MatchEndpoint), link.Href);

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.Host, ApiHost, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"拒绝请求非 Sony API 主机：{SonySecretRedactor.RedactUri(uri)}");
        return uri;
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
            Timeout = TimeSpan.FromSeconds(90)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("XperiaFukuro/0.1");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/xml");
        return client;
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _http.Dispose();
    }
}

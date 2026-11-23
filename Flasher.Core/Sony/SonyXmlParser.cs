using System.Globalization;
using System.Xml.Linq;

namespace Flasher.Core.Sony;

/// <summary>Sony ESS XML 解析器。只依赖元素本地名，兼容后续命名空间调整。</summary>
public static class SonyXmlParser
{
    public static SonyMatchResponse ParseMatch(string xml)
    {
        var root = ParseRoot(xml, "match-response");
        var services = new List<SonyServiceInfo>();
        var infos = Child(root, "device-service-infos");
        if (infos is not null)
        {
            foreach (var item in Children(infos, "software-device-service-info"))
                services.Add(ParseServiceInfo(item));
        }

        return new SonyMatchResponse
        {
            DeviceProblem = Text(root, "device-problem") ?? "",
            Services = services,
            RawXml = xml
        };
    }

    public static SonySoftwareService ParseService(string xml)
    {
        var root = ParseRoot(xml, "software-device-service");
        var files = new List<SonyFileResource>();
        var resources = Child(root, "file-resources");
        if (resources is not null)
        {
            foreach (var item in Children(resources, "file-resource"))
                files.Add(ParseFileResource(item));
        }

        return new SonySoftwareService
        {
            ProductId = Text(root, "product-id") ?? "",
            ProductName = Text(root, "product-name") ?? "",
            ModelId = Text(root, "model-id") ?? "",
            ModelName = Text(root, "model-name") ?? "",
            Name = Text(root, "name") ?? "",
            LogicType = Text(root, "logic-type") ?? "",
            Id = Text(root, "id") ?? "",
            SoftwareVersion = Text(root, "software-version") ?? "",
            Customer = Text(Child(root, "customization"), "customer") ?? "",
            AndroidVersion = Text(root, "android-version") ?? "",
            AndroidVersionUpdateType = Text(root, "android-version-update-type") ?? "",
            ReleaseState = Text(root, "release-state") ?? "",
            SecurityStateType = Text(root, "security-state-type") ?? "",
            Files = files,
            SelfLink = FindLink(root, "self"),
            RawXml = xml
        };
    }

    public static SonyFileManifest ParseManifest(string xml)
    {
        var root = ParseRoot(xml, "file-chunks");
        var chunks = new List<SonyFileChunk>();
        foreach (var item in Children(root, "file-chunk"))
        {
            var link = FindLink(item, "download")
                ?? throw new FormatException("file-chunk 缺少 download link");
            chunks.Add(new SonyFileChunk
            {
                Number = ParseInt(Attr(item, "number"), "file-chunk/@number"),
                Size = ParseLong(Text(item, "size"), "file-chunk/size"),
                DownloadLink = link
            });
        }

        if (chunks.Count == 0)
            throw new FormatException("file-chunks 没有分块");
        if (chunks.Select(chunk => chunk.Number).Distinct().Count() != chunks.Count)
            throw new FormatException("file-chunks 存在重复编号");

        return new SonyFileManifest
        {
            ContentId = Attr(root, "content-id"),
            Chunks = [.. chunks.OrderBy(chunk => chunk.Number)],
            RawXml = xml
        };
    }

    private static SonyServiceInfo ParseServiceInfo(XElement element)
    {
        var productCodes = new List<string>();
        var customization = Child(element, "customization");
        if (customization is not null)
        {
            foreach (var salesItem in Children(customization, "sales-item"))
            {
                var code = Text(salesItem, "sony-product-code");
                if (!string.IsNullOrWhiteSpace(code))
                    productCodes.Add(code);
            }
        }

        var counters = new List<SonyAntiRollbackCounter>();
        var counterRoot = Child(element, "anti-rollback-counters");
        if (counterRoot is not null)
        {
            foreach (var counter in Children(counterRoot, "anti-rollback-counter"))
            {
                var key = Text(counter, "key");
                var value = Text(counter, "value");
                if (!string.IsNullOrWhiteSpace(key) && value is not null)
                    counters.Add(new SonyAntiRollbackCounter(key, value));
            }
        }

        return new SonyServiceInfo
        {
            ProductId = Text(element, "product-id") ?? "",
            ProductName = Text(element, "product-name") ?? "",
            ModelId = Text(element, "model-id") ?? "",
            ModelName = Text(element, "model-name") ?? "",
            Name = Text(element, "name") ?? "",
            LogicType = Text(element, "logic-type") ?? "",
            ReleaseState = Text(element, "release-state") ?? "",
            Id = Text(element, "id") ?? "",
            MetaServiceKey = Text(element, "meta-service-key") ?? "",
            SoftwareVersion = Text(element, "software-version") ?? "",
            Owner = Text(element, "owner") ?? "",
            Category = Text(element, "category") ?? "",
            AndroidVersion = Text(element, "android-version") ?? "",
            AndroidVersionUpdateType = Text(element, "android-version-update-type") ?? "",
            Customer = Text(customization, "customer") ?? "",
            ProductCodes = productCodes,
            AntiRollbackCounters = counters,
            SelfLink = FindLink(element, "self")
        };
    }

    private static SonyFileResource ParseFileResource(XElement element)
    {
        return new SonyFileResource
        {
            ContentId = Attr(element, "content-id"),
            FileKey = Text(element, "file-key") ?? "",
            Name = Text(element, "name") ?? "",
            PlainSize = ParseLong(Text(element, "plain-size"), "file-resource/plain-size"),
            FileId = Text(element, "file-id"),
            FileVersion = Text(element, "file-version"),
            Gzipped = ParseBool(Text(element, "gzipped")),
            ReleaseState = Text(element, "release-state"),
            ChunkInfoLink = FindLink(element, "file-chunk-info")
        };
    }

    private static XElement ParseRoot(string xml, string expectedLocalName)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new FormatException("XML 为空");
        XDocument document;
        try
        {
            document = XDocument.Parse(xml, LoadOptions.None);
        }
        catch (Exception ex)
        {
            throw new FormatException($"XML 解析失败：{ex.Message}", ex);
        }

        var root = document.Root ?? throw new FormatException("XML 没有根元素");
        if (!string.Equals(root.Name.LocalName, expectedLocalName, StringComparison.Ordinal))
            throw new FormatException($"期望根元素 {expectedLocalName}，实际为 {root.Name.LocalName}");
        return root;
    }

    private static XElement? Child(XElement? parent, string name)
    {
        return parent?.Elements().FirstOrDefault(element =>
            string.Equals(element.Name.LocalName, name, StringComparison.Ordinal));
    }

    private static IEnumerable<XElement> Children(XElement parent, string name)
    {
        return parent.Elements().Where(element =>
            string.Equals(element.Name.LocalName, name, StringComparison.Ordinal));
    }

    private static string? Text(XElement? parent, string name)
    {
        return Child(parent, name)?.Value.Trim();
    }

    private static string? Attr(XElement element, string name)
    {
        return element.Attributes()
            .FirstOrDefault(attribute =>
                string.Equals(attribute.Name.LocalName, name, StringComparison.Ordinal))
            ?.Value;
    }

    private static SonyLink? FindLink(XElement parent, string rel)
    {
        return parent.Elements()
            .Where(element => string.Equals(element.Name.LocalName, "link", StringComparison.Ordinal))
            .Select(element => new SonyLink(
                Attr(element, "rel") ?? "",
                Attr(element, "href") ?? "",
                Attr(element, "type")))
            .FirstOrDefault(link =>
                string.Equals(link.Rel, rel, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(link.Href));
    }

    private static int ParseInt(string? value, string field)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new FormatException($"{field} 不是整数：{value}");
    }

    private static long ParseLong(string? value, string field)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new FormatException($"{field} 不是整数：{value}");
    }

    private static bool ParseBool(string? value)
    {
        return bool.TryParse(value, out var result) && result;
    }
}

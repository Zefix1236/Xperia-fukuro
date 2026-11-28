using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flasher.Core.Catalog;

/// <summary>Sony 设备目录：产品、型号与可用 SPC。</summary>
public sealed class SonyCatalog
{
    public const int SupportedSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private SonyCatalog(SonyCatalogDocument document)
    {
        Document = document;
    }

    public SonyCatalogDocument Document { get; }

    public IReadOnlyList<SonyProduct> Products => Document.Products;

    public IEnumerable<(SonyProduct Product, SonyModel Model)> EnumerateModels()
    {
        foreach (var product in Products)
        foreach (var model in product.Models)
            yield return (product, model);
    }

    public SonyProduct? FindProduct(string productCodeOrMarketingName)
    {
        return Products.FirstOrDefault(product =>
            string.Equals(product.ProductCode, productCodeOrMarketingName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                product.MarketingName,
                productCodeOrMarketingName,
                StringComparison.OrdinalIgnoreCase));
    }

    public static SonyCatalog LoadEmbedded()
    {
        var assembly = typeof(SonyCatalog).Assembly;
        const string resourceName = "Flasher.Core.Catalog.Data.devices.sony.json";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"找不到内嵌设备目录：{resourceName}");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    public static SonyCatalog Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var document = JsonSerializer.Deserialize<SonyCatalogDocument>(json, JsonOptions)
            ?? throw new InvalidDataException("设备目录为空");
        if (document.SchemaVersion != SupportedSchemaVersion)
            throw new InvalidDataException($"不支持的设备目录版本：{document.SchemaVersion}");
        if (document.Products.Count == 0)
            throw new InvalidDataException("设备目录没有产品");

        var productIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var modelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var product in document.Products)
        {
            if (string.IsNullOrWhiteSpace(product.ProductId)
                || string.IsNullOrWhiteSpace(product.ProductCode)
                || string.IsNullOrWhiteSpace(product.MarketingName))
                throw new InvalidDataException("产品缺少 ID、代号或名称");
            if (!productIds.Add(product.ProductId))
                throw new InvalidDataException($"产品 ID 重复：{product.ProductId}");

            foreach (var model in product.Models)
            {
                if (string.IsNullOrWhiteSpace(model.ModelId)
                    || string.IsNullOrWhiteSpace(model.ModelName)
                    || string.IsNullOrWhiteSpace(model.HwVariantId))
                    throw new InvalidDataException($"型号缺少 ID 或 HW 变体：{product.ProductCode}/{model.ModelName}");
                if (!modelIds.Add(model.ModelId))
                    throw new InvalidDataException($"型号 ID 重复：{model.ModelId}");
            }
        }

        return new SonyCatalog(document);
    }
}

public sealed class SonyCatalogDocument
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; }

    [JsonPropertyName("generatedAt")]
    public string GeneratedAt { get; init; } = "";

    [JsonPropertyName("sources")]
    public List<SonyCatalogSource> Sources { get; init; } = [];

    [JsonPropertyName("products")]
    public List<SonyProduct> Products { get; init; } = [];
}

public sealed class SonyCatalogSource
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("url")]
    public string Url { get; init; } = "";

    [JsonPropertyName("fetchedAt")]
    public string? FetchedAt { get; init; }

    [JsonPropertyName("author")]
    public string? Author { get; init; }

    [JsonPropertyName("date")]
    public string? Date { get; init; }

    [JsonPropertyName("note")]
    public string? Note { get; init; }
}

public sealed class SonyProduct
{
    [JsonPropertyName("productId")]
    public string ProductId { get; init; } = "";

    [JsonPropertyName("productCode")]
    public string ProductCode { get; init; } = "";

    [JsonPropertyName("marketingName")]
    public string MarketingName { get; init; } = "";

    [JsonPropertyName("series")]
    public string Series { get; init; } = "";

    [JsonPropertyName("generation")]
    public string Generation { get; init; } = "";

    [JsonPropertyName("updatePlatformKey")]
    public string? UpdatePlatformKey { get; init; }

    [JsonPropertyName("releaseYear")]
    public int ReleaseYear { get; init; }

    [JsonPropertyName("models")]
    public List<SonyModel> Models { get; init; } = [];

    public string DisplayName => $"{MarketingName}（{ProductCode}）";
}

public sealed class SonyCustomization
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("spc")]
    public string Spc { get; init; } = "";
}

public sealed class SonyModel
{
    [JsonPropertyName("modelId")]
    public string ModelId { get; init; } = "";

    [JsonPropertyName("modelName")]
    public string ModelName { get; init; } = "";

    [JsonPropertyName("hwVariantId")]
    public string HwVariantId { get; init; } = "";

    [JsonPropertyName("eos")]
    public bool Eos { get; init; }

    [JsonPropertyName("tacs")]
    public List<string> Tacs { get; init; } = [];

    [JsonPropertyName("aliases")]
    public string? Aliases { get; init; }

    [JsonPropertyName("customisationName")]
    public string? CustomisationName { get; init; }

    [JsonPropertyName("spcs")]
    public List<string> Spcs { get; init; } = [];

    [JsonPropertyName("spcRequired")]
    public bool SpcRequired { get; init; }

    [JsonPropertyName("customizations")]
    public List<SonyCustomization> Customizations { get; init; } = [];

    public bool HasSpc => Spcs.Count > 0;

    public string DisplayName => string.IsNullOrWhiteSpace(CustomisationName)
        ? ModelName
        : $"{ModelName} · {CustomisationName}";
}

using Flasher.Core.Catalog;

namespace Flasher.Core.Sony;

/// <summary>match 请求所需的设备上下文。</summary>
public sealed record SonyDeviceSelection(
    string ProductId,
    string ModelId,
    string HwVariantId,
    string SonyProductCode,
    string DisplayName)
{
    public static SonyDeviceSelection From(SonyProduct product, SonyModel model, string? spc = null)
    {
        var selectedSpc = spc ?? model.Spcs.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(selectedSpc))
            throw new InvalidOperationException($"型号 {model.ModelName} 没有可用的 SPC，请手动输入");
        return new SonyDeviceSelection(
            product.ProductId,
            model.ModelId,
            model.HwVariantId,
            selectedSpc,
            $"{product.MarketingName} {model.ModelName}");
    }

    public void Validate()
    {
        if (!Guid.TryParse(ProductId, out _))
            throw new ArgumentException("ProductId 不是合法 UUID", nameof(ProductId));
        if (!Guid.TryParse(ModelId, out _))
            throw new ArgumentException("ModelId 不是合法 UUID", nameof(ModelId));
        if (!Guid.TryParse(HwVariantId, out _))
            throw new ArgumentException("HwVariantId 不是合法 UUID", nameof(HwVariantId));
        if (string.IsNullOrWhiteSpace(SonyProductCode))
            throw new ArgumentException("SonyProductCode 不能为空", nameof(SonyProductCode));
    }
}

public sealed record SonyLink(string Rel, string Href, string? Type = null);

public sealed class SonyMatchResponse
{
    public string DeviceProblem { get; init; } = "";
    public List<SonyServiceInfo> Services { get; init; } = [];
    public string RawXml { get; init; } = "";
}

public sealed class SonyServiceInfo
{
    public string ProductId { get; init; } = "";
    public string ProductName { get; init; } = "";
    public string ModelId { get; init; } = "";
    public string ModelName { get; init; } = "";
    public string Name { get; init; } = "";
    public string LogicType { get; init; } = "";
    public string ReleaseState { get; init; } = "";
    public string Id { get; init; } = "";
    public string MetaServiceKey { get; init; } = "";
    public string SoftwareVersion { get; init; } = "";
    public string Owner { get; init; } = "";
    public string Category { get; init; } = "";
    public string AndroidVersion { get; init; } = "";
    public string AndroidVersionUpdateType { get; init; } = "";
    public string Customer { get; init; } = "";
    public List<string> ProductCodes { get; init; } = [];
    public List<SonyAntiRollbackCounter> AntiRollbackCounters { get; init; } = [];
    public SonyLink? SelfLink { get; init; }

    public bool IsTarget =>
        string.Equals(AndroidVersionUpdateType, "TARGET", StringComparison.OrdinalIgnoreCase);

    public bool IsContentErase =>
        string.Equals(LogicType, "SOFTWARE_UPDATE_CONTENT_ERASE", StringComparison.OrdinalIgnoreCase);
}

public sealed record SonyAntiRollbackCounter(string Key, string Value);

public sealed class SonySoftwareService
{
    public string ProductId { get; init; } = "";
    public string ProductName { get; init; } = "";
    public string ModelId { get; init; } = "";
    public string ModelName { get; init; } = "";
    public string Name { get; init; } = "";
    public string LogicType { get; init; } = "";
    public string Id { get; init; } = "";
    public string SoftwareVersion { get; init; } = "";
    public string Customer { get; init; } = "";
    public string AndroidVersion { get; init; } = "";
    public string AndroidVersionUpdateType { get; init; } = "";
    public string ReleaseState { get; init; } = "";
    public string SecurityStateType { get; init; } = "";
    public List<SonyFileResource> Files { get; init; } = [];
    public SonyLink? SelfLink { get; init; }
    public string RawXml { get; init; } = "";
}

public sealed class SonyFileResource
{
    public string? ContentId { get; init; }
    public string FileKey { get; init; } = "";
    public string Name { get; init; } = "";
    public long PlainSize { get; init; }
    public string? FileId { get; init; }
    public string? FileVersion { get; init; }
    public bool Gzipped { get; init; }
    public string? ReleaseState { get; init; }
    public SonyLink? ChunkInfoLink { get; init; }

    public bool IsFirmwarePackage =>
        string.Equals(FileKey, "APP_SW", StringComparison.OrdinalIgnoreCase);
}

public sealed class SonyFileManifest
{
    public string? ContentId { get; init; }
    public List<SonyFileChunk> Chunks { get; init; } = [];
    public string RawXml { get; init; } = "";

    public long TotalSize => Chunks.Sum(chunk => chunk.Size);
}

public sealed class SonyFileChunk
{
    public int Number { get; init; }
    public long Size { get; init; }
    public SonyLink DownloadLink { get; init; } = new("download", "");
}

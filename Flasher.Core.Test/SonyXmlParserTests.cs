using Flasher.Core.Sony;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flasher.Core.Test;

[TestClass]
public sealed class SonyXmlParserTests
{
    [TestMethod]
    public void ParsesMatchResponse()
    {
        var response = SonyXmlParser.ParseMatch(Fixture("match.xml"));

        Assert.AreEqual("NO_PROBLEM", response.DeviceProblem);
        Assert.AreEqual(6, response.Services.Count);
        Assert.IsTrue(response.Services.All(service => service.SelfLink is not null));

        var target = response.Services.First(service =>
            service.Id == "28984678-COMMERCIAL_SUCE_GEN2");
        Assert.AreEqual("69.2.A.4.110", target.SoftwareVersion);
        Assert.AreEqual("16", target.AndroidVersion);
        Assert.IsTrue(target.IsTarget);
        Assert.IsTrue(target.IsContentErase);
        Assert.AreEqual(10, target.ProductCodes.Count);
    }

    [TestMethod]
    public void ParsesSoftwareService()
    {
        var service = SonyXmlParser.ParseService(Fixture("service.xml"));

        Assert.AreEqual("28984678-COMMERCIAL_SUCE_GEN2", service.Id);
        Assert.AreEqual("XQ-EC44_Customized_JP", service.Customer);
        Assert.AreEqual(3, service.Files.Count);

        var app = service.Files.Single(file => file.FileKey == "APP_SW");
        Assert.AreEqual("21037141", app.ContentId);
        Assert.AreEqual("S20001053", app.FileId);
        Assert.AreEqual(4_403_481_798L, app.PlainSize);
        Assert.IsNotNull(app.ChunkInfoLink);
        Assert.IsFalse(app.Gzipped);
    }

    [TestMethod]
    public void ParsesAppSwManifest()
    {
        var manifest = SonyXmlParser.ParseManifest(Fixture("manifest-appsw.xml"));

        Assert.AreEqual("21037141", manifest.ContentId);
        Assert.AreEqual(42, manifest.Chunks.Count);
        Assert.AreEqual(4_403_481_798L, manifest.TotalSize);
        Assert.AreEqual(0, manifest.Chunks[0].Number);
        Assert.AreEqual(104_857_600L, manifest.Chunks[0].Size);
        Assert.AreEqual(41, manifest.Chunks[^1].Number);
        Assert.AreEqual(104_320_198L, manifest.Chunks[^1].Size);
    }

    [TestMethod]
    public void RejectsWrongRoot()
    {
        Assert.ThrowsExactly<FormatException>(
            () => SonyXmlParser.ParseManifest("<match-response />"));
    }

    private static string Fixture(string name)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Sony", name));
    }
}

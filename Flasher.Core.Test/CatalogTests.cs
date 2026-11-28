using Flasher.Core.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flasher.Core.Test;

[TestClass]
public sealed class CatalogTests
{
    [TestMethod]
    public void EmbeddedCatalogContainsNewGenerationDevices()
    {
        var catalog = SonyCatalog.LoadEmbedded();

        Assert.AreEqual(42, catalog.Products.Count);
        Assert.AreEqual(210, catalog.Products.Sum(product => product.Models.Count));
        Assert.AreEqual(108, catalog.EnumerateModels().Count(pair => pair.Model.HasSpc));

        var xperia1Vi = catalog.FindProduct("PDX-245");
        Assert.IsNotNull(xperia1Vi);
        Assert.AreEqual("Xperia 1 VI", xperia1Vi.MarketingName);

        var xqEc44 = xperia1Vi.Models.Single(model => model.ModelName == "XQ-EC44");
        Assert.IsTrue(xqEc44.HasSpc);
        Assert.AreEqual(10, xqEc44.Spcs.Count);
        Assert.AreEqual("Customized JP", xqEc44.CustomisationName);

        var tenViii = catalog.FindProduct("PDX-268");
        Assert.IsNotNull(tenViii);
        var xqGh44 = tenViii.Models.Single(model => model.ModelName == "XQ-GH44");
        Assert.IsFalse(xqGh44.SpcRequired);
        Assert.IsTrue(xqGh44.HasSpc);
    }

    [TestMethod]
    public void ParseRejectsUnknownSchema()
    {
        const string json = """{"schemaVersion":99,"products":[]}""";
        Assert.ThrowsExactly<InvalidDataException>(() => SonyCatalog.Parse(json));
    }
}

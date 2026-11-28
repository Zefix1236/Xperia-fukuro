using Flasher.Core.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flasher.Core.Test;

[TestClass]
public sealed class SonyProductSearchTests
{
    private static readonly SonyCatalog Catalog = SonyCatalog.LoadEmbedded();

    [TestMethod]
    public void NormalizeDropsSeparatorsAndCase()
    {
        Assert.AreEqual("pdx245", SonyProductSearch.Normalize("PDX-245"));
        Assert.AreEqual("xqec44", SonyProductSearch.Normalize("XQ-EC44"));
        Assert.AreEqual("xperia1iii", SonyProductSearch.Normalize("Xperia 1 III"));
        Assert.AreEqual("马克3", SonyProductSearch.Normalize("马克 3"));
    }

    [TestMethod]
    public void MatchesProductCodeWithOrWithoutSeparator()
    {
        var product = Get("PDX-245");

        Assert.IsTrue(SonyProductSearch.Matches(product, "pdx245"));
        Assert.IsTrue(SonyProductSearch.Matches(product, "pdx 245"));
        Assert.IsTrue(SonyProductSearch.Matches(product, "PDX-245"));
    }

    [TestMethod]
    public void MatchesModelCodeWithOrWithoutSeparator()
    {
        var product = Get("PDX-245");

        Assert.IsTrue(SonyProductSearch.Matches(product, "xqec44"));
        Assert.IsTrue(SonyProductSearch.Matches(product, "XQ-EC44"));
    }

    [TestMethod]
    public void BareNumberMatchesGeneration()
    {
        Assert.IsTrue(SonyProductSearch.Matches(Get("Griffin"), "1"));
        Assert.IsFalse(SonyProductSearch.Matches(Get("PDX-234"), "1"));
        Assert.IsFalse(SonyProductSearch.Matches(Get("PDX-213"), "1"));

        Assert.IsTrue(SonyProductSearch.Matches(Get("PDX-234"), "5"));
        Assert.IsTrue(SonyProductSearch.Matches(Get("PDX-237"), "5"));
        Assert.IsTrue(SonyProductSearch.Matches(Get("PDX-235"), "5"));
        Assert.IsFalse(SonyProductSearch.Matches(Get("Bahamut"), "5"));

        Assert.IsTrue(SonyProductSearch.Matches(Get("Kirin"), "10"));
        Assert.IsTrue(SonyProductSearch.Matches(Get("PDX-235"), "10"));
        Assert.IsFalse(SonyProductSearch.Matches(Get("PDX-234"), "10"));
    }

    [TestMethod]
    public void BareNumberAlsoMatchesLegacyModelNumbers()
    {
        Assert.IsTrue(SonyProductSearch.Matches(Get("Akari"), "2"));
        Assert.IsTrue(SonyProductSearch.Matches(Get("Pioneer"), "2"));
        Assert.IsTrue(SonyProductSearch.Matches(Get("Ranger"), "2"));
        Assert.IsFalse(SonyProductSearch.Matches(Get("PDX-234"), "2"));
    }

    [TestMethod]
    public void MatchesRomanAndArabicGenerationWithoutCrossMatchingSeries()
    {
        var oneThree = Get("PDX-215");
        var tenThree = Get("PDX-213");

        Assert.IsTrue(SonyProductSearch.Matches(oneThree, "xperia 1 3"));
        Assert.IsFalse(SonyProductSearch.Matches(tenThree, "xperia 1 3"));

        Assert.IsTrue(SonyProductSearch.Matches(tenThree, "xperia 10 3"));
        Assert.IsFalse(SonyProductSearch.Matches(oneThree, "xperia 10 3"));
    }

    [TestMethod]
    public void MatchesMarkAliases()
    {
        var oneThree = Get("PDX-215");
        var tenThree = Get("PDX-213");

        Assert.IsTrue(SonyProductSearch.Matches(oneThree, "mark 3"));
        Assert.IsTrue(SonyProductSearch.Matches(tenThree, "mark 3"));
        Assert.IsTrue(SonyProductSearch.Matches(oneThree, "马克3"));
    }

    [TestMethod]
    public void NumericShorthandMatchesOnlyItsSeriesAndGeneration()
    {
        var matches = Catalog.Products
            .Where(product => SonyProductSearch.Matches(product, "15"))
            .Select(product => product.ProductCode)
            .ToList();
        CollectionAssert.AreEqual(new[] { "PDX-234" }, matches);

        Assert.IsTrue(SonyProductSearch.Matches(Get("PDX-234"), "1 5"));
        Assert.IsTrue(SonyProductSearch.Matches(Get("PDX-215"), "215"));
        Assert.IsFalse(SonyProductSearch.Matches(Get("PDX-215"), "15"));
        Assert.IsTrue(SonyProductSearch.Matches(Get("PDX-256"), "sog15"));
        Assert.IsFalse(SonyProductSearch.Matches(Get("PDX-256"), "15"));
    }

    [TestMethod]
    public void GenerationShorthandMatchesOnlyExactGeneration()
    {
        var oneFive = Get("PDX-234");
        var oneSix = Get("PDX-245");
        var oneFour = Get("PDX-223");

        Assert.IsTrue(SonyProductSearch.Matches(oneFive, "1v"));
        Assert.IsFalse(SonyProductSearch.Matches(oneSix, "1v"));
        Assert.IsFalse(SonyProductSearch.Matches(oneFour, "1v"));

        Assert.IsTrue(SonyProductSearch.Matches(oneFive, "v"));
        Assert.IsTrue(SonyProductSearch.Matches(Get("PDX-237"), "v"));
        Assert.IsTrue(SonyProductSearch.Matches(Get("PDX-235"), "v"));
        Assert.IsFalse(SonyProductSearch.Matches(oneSix, "v"));
        Assert.IsFalse(SonyProductSearch.Matches(oneFour, "v"));

        Assert.IsTrue(SonyProductSearch.Matches(oneSix, "1vi"));
        Assert.IsFalse(SonyProductSearch.Matches(oneFive, "1vi"));
    }

    [TestMethod]
    public void MatchesMultipleTermsInOneField()
    {
        var product = Get("PDX-245");

        Assert.IsTrue(SonyProductSearch.Matches(product, "xperia 1 vi"));
        Assert.IsTrue(SonyProductSearch.Matches(product, "1 vi"));
    }

    [TestMethod]
    public void YearFilterIsAndCondition()
    {
        var product = Get("PDX-215");

        Assert.IsTrue(SonyProductSearch.Matches(product, "1iii", 2021));
        Assert.IsFalse(SonyProductSearch.Matches(product, "1iii", 2022));
        Assert.IsFalse(SonyProductSearch.Matches(product, "", 2022));
        Assert.IsTrue(SonyProductSearch.Matches(product, "", 2021));
    }

    private static SonyProduct Get(string productCode)
    {
        return Catalog.FindProduct(productCode)
            ?? throw new AssertFailedException($"Missing product {productCode}");
    }
}

using Flasher.Core.Sony;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flasher.Core.Test;

[TestClass]
public sealed class SonyApiClientTests
{
    [TestMethod]
    public void BuildMatchUriContainsExpectedParameters()
    {
        var selection = new SonyDeviceSelection(
            "c2cb0a95-7fee-4370-8982-319473d61399",
            "ce95872f-701d-49ba-9960-62515d055bc9",
            "ca35bf60-53f3-45c3-9d7b-8c414bbf381e",
            "43041336",
            "Xperia 1 VI XQ-EC44");

        var uri = SonyApiClient.BuildMatchUri(selection);
        var parameters = ParseQuery(uri);

        Assert.AreEqual(6, parameters.Count);
        Assert.AreEqual("Default", parameters["HwSetupKey"]);
        Assert.AreEqual(selection.HwVariantId, parameters["HWVariantId"]);
        Assert.AreEqual(selection.ModelId, parameters["ModelObjectId"]);
        Assert.AreEqual(selection.ProductId, parameters["ProductObjectId"]);
        Assert.AreEqual("COMMERCIAL", parameters["SecurityStateType"]);
        Assert.AreEqual(selection.SonyProductCode, parameters["SonyProductCode"]);
        Assert.IsFalse(uri.Query.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void RedactsCredentialsAndStorageTokens()
    {
        const string input =
            "https://app.swup.update.sony.net/x?password=secret&MiscPassword=secret2&CeParam=abc%3D" +
            " https://app.swup.update.sony.net/filestorage/21037141/0123456789abcdef_part0";
        var redacted = SonySecretRedactor.Redact(input);

        Assert.IsFalse(redacted.Contains("secret", StringComparison.Ordinal));
        Assert.IsFalse(redacted.Contains("abc%3D", StringComparison.Ordinal));
        Assert.IsFalse(redacted.Contains("0123456789abcdef", StringComparison.Ordinal));
        Assert.IsTrue(redacted.Contains("password=REDACTED", StringComparison.Ordinal));
        Assert.IsTrue(redacted.Contains("REDACTED_part0", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RejectsNonSonyApiHost()
    {
        var link = new SonyLink("self", "https://example.com/evil");
        using var client = new SonyApiClient(new HttpClient());
        Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await client.GetServiceAsync(link));
    }

    private static Dictionary<string, string> ParseQuery(Uri uri)
    {
        return uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(
                pair => Uri.UnescapeDataString(pair[0]),
                pair => pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : "");
    }
}

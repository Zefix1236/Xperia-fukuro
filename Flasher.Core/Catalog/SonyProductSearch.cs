using System.Text;
using System.Text.RegularExpressions;

namespace Flasher.Core.Catalog;

/// <summary>XperiFirm 机型搜索：忽略大小写、空格与连字符，支持多关键词和罗马数字别名。</summary>
public static class SonyProductSearch
{
    public static bool Matches(SonyProduct product, string? query, int year = 0)
    {
        if (year > 0 && product.ReleaseYear != year)
            return false;

        var terms = ParseTerms(query);
        if (terms.Count == 0)
            return true;

        if (terms.Count == 1 && IsBaseSeriesNumber(terms[0]))
            return string.Equals(
                Normalize(product.MarketingName),
                $"xperia{terms[0]}",
                StringComparison.Ordinal);

        foreach (var field in EnumerateFields(product))
        {
            if (terms.Count == 1)
            {
                if (IsAsciiDigits(terms[0]) || IsGenerationShorthand(terms[0]))
                {
                    if (field.Tokens.Contains(terms[0], StringComparer.Ordinal))
                        return true;
                }
                else if (field.Compact.Contains(terms[0], StringComparison.Ordinal))
                {
                    return true;
                }
            }
            else if (terms.All(term => field.Tokens.Contains(term, StringComparer.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>归一化：全角转半角、小写，并移除空白、连字符等分隔符。</summary>
    public static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (IsSeparator(ch))
                continue;
            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }

    private static List<string> ParseTerms(string? query)
    {
        var terms = new List<string>();
        if (string.IsNullOrWhiteSpace(query))
            return terms;

        foreach (var part in query.Split(
                     (char[]?)null,
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalized = Normalize(part);
            if (normalized.Length > 0)
                terms.Add(normalized);
        }

        return terms;
    }

    private static IEnumerable<SearchField> EnumerateFields(SonyProduct product)
    {
        yield return CreateField(product.MarketingName);
        yield return CreateField(product.ProductCode);

        foreach (var model in product.Models)
            yield return CreateField(model.ModelName);

        var generation = RomanToNumber(product.Generation);
        if (generation <= 0)
            yield break;

        var series = product.Series;
        yield return CreateField(product.Generation);
        yield return CreateField($"{series}{product.Generation}");
        yield return CreateField($"Xperia {series} {generation}");
        yield return CreateField($"{series} {generation}");
        yield return CreateField($"{series}{generation}");
        yield return CreateField($"Xperia {series} Mark {generation}");
        yield return CreateField($"{series} Mark {generation}");
        yield return CreateField($"Mark {generation}");
        yield return CreateField($"MK {generation}");
        yield return CreateField($"马克{generation}");
    }

    private static SearchField CreateField(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKC);
        var compact = new StringBuilder(normalized.Length);
        var tokens = new List<string>();
        var token = new StringBuilder();

        foreach (var ch in normalized)
        {
            if (IsSeparator(ch))
            {
                FlushToken();
                continue;
            }

            var lower = char.ToLowerInvariant(ch);
            compact.Append(lower);
            token.Append(lower);
        }

        FlushToken();
        return new SearchField(compact.ToString(), tokens.ToArray());

        void FlushToken()
        {
            if (token.Length == 0)
                return;
            tokens.Add(token.ToString());
            token.Clear();
        }
    }

    private static bool IsBaseSeriesNumber(string value)
    {
        return value is "1" or "5" or "10";
    }

    private static bool IsGenerationShorthand(string value)
    {
        return Regex.IsMatch(
            value,
            "^(?:i|ii|iii|iv|v|vi|vii|viii|ix|x|(?:1|5|10)(?:i|ii|iii|iv|v|vi|vii|viii|ix|x))$",
            RegexOptions.CultureInvariant);
    }

    private static bool IsAsciiDigits(string value)
    {
        return value.Length > 0 && value.All(ch => ch is >= '0' and <= '9');
    }

    private static bool IsSeparator(char ch)
    {
        return char.IsWhiteSpace(ch) || ch is '-' or '_' or '·' or '—' or '–';
    }

    private static int RomanToNumber(string generation)
    {
        return generation.ToUpperInvariant() switch
        {
            "I" => 1,
            "II" => 2,
            "III" => 3,
            "IV" => 4,
            "V" => 5,
            "VI" => 6,
            "VII" => 7,
            "VIII" => 8,
            "IX" => 9,
            "X" => 10,
            _ => 0
        };
    }

    private sealed record SearchField(string Compact, string[] Tokens);
}

using DocuSpend.Categorization.Rules;

namespace DocuSpend.Categorization.Tests;

public class TextMatchingTests
{
    [Theory]
    [InlineData("LEKÁREŇ", "lekaren")]
    [InlineData("Škôlka", "skolka")]
    [InlineData("Ľubomír Šťastný", "lubomir stastny")]
    [InlineData("  DM   DROGERIE\tMARKT \n", " dm drogerie markt ")]
    [InlineData("", "")]
    public void Folding_ignores_case_and_diacritics_and_collapses_spaces(string text, string expected) =>
        Assert.Equal(expected, TextMatching.Fold(text));

    [Theory]
    [InlineData("COOP Jednota Nitra", "coop", true)]
    [InlineData("coop jednota", "COOP", true)]
    [InlineData("LEKÁREŇ PRI PARKU", "lekaren", true)]
    [InlineData("Platba za LEKAREN", "LEKÁREŇ", true)]
    [InlineData("DM   DROGERIE   MARKT", "dm drogerie", true)]
    [InlineData("KAUFLAND", "lidl", false)]
    [InlineData("", "lidl", false)]
    public void Contains_ignores_case_diacritics_and_extra_spaces(string text, string pattern, bool expected) =>
        Assert.Equal(expected, TextMatching.Contains(text, pattern));

    [Theory]
    [InlineData("OBI Nitra", "OBI ", true)]
    [InlineData("Nákup v OBI", "OBI ", true)]
    [InlineData("OBI", "OBI ", true)]
    [InlineData("OBILNINY SKLAD", "OBI ", false)]
    [InlineData("MOL Bratislava", "MOL ", true)]
    [InlineData("MOLDAVSKA CESTA", "MOL ", false)]
    [InlineData("Platba MOL", "MOL ", true)]
    [InlineData("DOMOL BRATISLAVA", "MOL ", true)]
    [InlineData("OBI Nitra", " OBI ", true)]
    [InlineData("Nákup v OBI", " OBI ", true)]
    [InlineData("OBI", " OBI ", true)]
    [InlineData("KOBI SKLAD", " OBI ", false)]
    [InlineData("ZLOBI SA", " OBI ", false)]
    [InlineData("DOMOL BRATISLAVA", " MOL ", false)]
    [InlineData("MOL Bratislava", " MOL ", true)]
    [InlineData("COOP Jednota", " COOP", true)]
    [InlineData("SCOOP Cafe", " COOP", false)]
    [InlineData("Coop", " COOP", true)]
    public void A_space_in_a_pattern_marks_a_word_edge_even_at_the_ends_of_the_text(string text, string pattern, bool expected) =>
        Assert.Equal(expected, TextMatching.Contains(text, pattern));

    [Theory]
    [InlineData("Mol.Benzinka Nitra", " MOL ", true)]
    [InlineData("MOL,BENZINKA", " MOL ", true)]
    [InlineData("Platba (MOL)", " MOL ", true)]
    [InlineData("Dr.Max 123 Nitra", "DR.MAX", true)]
    [InlineData("Dr.Max 123 Nitra", "DR MAX", true)]
    [InlineData("DR-MAX 123", "DR.MAX", true)]
    [InlineData("www.mall.sk/objednavka", "MALL.SK", true)]
    [InlineData("GOOGLE*YouTubePremium", "YOUTUBEPREMIUM", true)]
    [InlineData("GOOGLE *YouTube Premium", "YOUTUBE PREMIUM", true)]
    [InlineData("Bistro.sk, s. r. o.", "BISTRO", true)]
    [InlineData("DOMOL.BRATISLAVA", " MOL ", false)]
    [InlineData("MOLDAVSKA.CESTA", " MOL ", false)]
    [InlineData("SCOOP.CAFE", " COOP", false)]
    public void In_contains_mode_punctuation_counts_as_a_space(string text, string pattern, bool expected) =>
        Assert.Equal(expected, TextMatching.Contains(text, pattern));

    [Theory]
    [InlineData("...")]
    [InlineData(" . , ")]
    [InlineData("***")]
    public void A_pattern_of_only_punctuation_matches_nothing(string pattern) =>
        Assert.False(TextMatching.Contains("Platba . , *** ...", pattern));

    [Theory]
    [InlineData("...", true)]
    [InlineData(" * ", true)]
    [InlineData("", true)]
    [InlineData("MOL.", false)]
    [InlineData("OBI ", false)]
    public void A_pattern_without_letters_or_digits_has_no_words(string pattern, bool expected) =>
        Assert.Equal(expected, TextMatching.HasNoWords(pattern));

    [Theory]
    [InlineData("MZDA.", "MZDA", false)]
    [InlineData("MZDA", "MZDA.", false)]
    public void Equals_mode_is_not_affected_by_punctuation(string text, string value, bool expected) =>
        Assert.Equal(expected, TextMatching.Equal(text, value));

    [Theory]
    [InlineData("MZDA", "mzda", true)]
    [InlineData("  Mzda  ", "MZDA", true)]
    [InlineData("MZDA 02/2026", "MZDA", false)]
    [InlineData("Lekáreň", "LEKAREN", true)]
    [InlineData("", "", false)]
    public void Equals_compares_whole_texts_apart_from_surrounding_spaces(string text, string value, bool expected) =>
        Assert.Equal(expected, TextMatching.Equal(text, value));

    [Theory]
    [InlineData("SK31 0200 0000 0012 3456 7890", "sk3102000000001234567890")]
    [InlineData(" sk31  0200 ", "SK310200")]
    public void Account_numbers_are_compared_without_spaces_and_case(string a, string b) =>
        Assert.True(TextMatching.SameAccount(a, b));

    [Fact]
    public void Different_account_numbers_are_different() =>
        Assert.False(TextMatching.SameAccount("SK31 0200 0000 0012 3456 7890", "SK31 0200 0000 0012 3456 7891"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Blank_patterns_are_blank(string pattern) => Assert.True(TextMatching.IsBlank(pattern));

    [Fact]
    public void A_pattern_with_only_a_trailing_space_is_not_blank() => Assert.False(TextMatching.IsBlank("OBI "));
}

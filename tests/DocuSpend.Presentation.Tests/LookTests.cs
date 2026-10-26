using DocuSpend.Categorization;
using DocuSpend.Categorization.Model;
using DocuSpend.Presentation.Look;

namespace DocuSpend.Presentation.Tests;

/// <summary>The look's colours: contrast in both looks, and a fixed colour per category (spec: readability, charts).</summary>
public class LookTests
{
    [Theory]
    [InlineData("#000000", "#FFFFFF", 21.0)]
    [InlineData("#FFFFFF", "#FFFFFF", 1.0)]
    [InlineData("#777777", "#FFFFFF", 4.48)]
    [InlineData("#0000FF", "#FFFFFF", 8.59)]
    public void Contrast_is_the_wcag_ratio(string a, string b, double expected)
    {
        Assert.Equal(expected, Contrast.Ratio(a, b), 2);
        Assert.Equal(Contrast.Ratio(a, b), Contrast.Ratio(b, a), 10);
    }

    public static TheoryData<string> Looks => new(["light", "dark"]);

    private static Palette Of(string look) => look == "light" ? Palette.Light : Palette.Dark;

    /// <summary>Every pair of colours the spec names, with its minimum: 4.5 to 1 for text, 3 to 1 for large text and graphics.</summary>
    public static TheoryData<string, string, string, double> Pairs
    {
        get
        {
            var data = new TheoryData<string, string, string, double>();
            foreach (var look in new[] { "light", "dark" })
            {
                data.Add(look, nameof(Palette.Text), nameof(Palette.Card), 4.5);
                data.Add(look, nameof(Palette.Text), nameof(Palette.Background), 4.5);
                data.Add(look, nameof(Palette.Muted), nameof(Palette.Card), 4.5);
                data.Add(look, nameof(Palette.Muted), nameof(Palette.Background), 4.5);
                data.Add(look, nameof(Palette.Positive), nameof(Palette.Card), 4.5);
                data.Add(look, nameof(Palette.Error), nameof(Palette.Card), 4.5);
                data.Add(look, nameof(Palette.Error), nameof(Palette.ErrorTint), 4.5);
                data.Add(look, nameof(Palette.Positive), nameof(Palette.PositiveTint), 4.5);
                data.Add(look, nameof(Palette.Text), nameof(Palette.AccentTint), 4.5);
                data.Add(look, nameof(Palette.Text), nameof(Palette.Hover), 4.5);
                data.Add(look, nameof(Palette.OnPrimary), nameof(Palette.Primary), 4.5);
                data.Add(look, nameof(Palette.Primary), nameof(Palette.Card), 3);
                data.Add(look, nameof(Palette.Accent), nameof(Palette.Card), 3);
                data.Add(look, nameof(Palette.Accent), nameof(Palette.Background), 3);
                // The outlined buttons' text, on the card and on the tint while the pointer is over them (#12).
                data.Add(look, nameof(Palette.Accent), nameof(Palette.Card), 4.5);
                data.Add(look, nameof(Palette.Accent), nameof(Palette.Hover), 4.5);
                // A message's close button, in the error colour, while the pointer is over it.
                data.Add(look, nameof(Palette.Error), nameof(Palette.Hover), 4.5);
                // A switch's choice under the pointer: readable, and visibly apart from the switch, whose background
                // is the rows' hover tone (#19). 1.15 is about how far the chosen pill stands from the switch.
                data.Add(look, nameof(Palette.Text), nameof(Palette.SwitchHover), 4.5);
                data.Add(look, nameof(Palette.SwitchHover), nameof(Palette.Hover), 1.15);
                // A scroll bar's handle, on a card, on the window's background and on its own track (#19).
                data.Add(look, nameof(Palette.ScrollThumb), nameof(Palette.Card), 3);
                data.Add(look, nameof(Palette.ScrollThumb), nameof(Palette.Background), 3);
                data.Add(look, nameof(Palette.ScrollThumb), nameof(Palette.Hover), 3);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public void Every_named_pair_is_readable(string look, string foreground, string background, double minimum)
    {
        var palette = Of(look);

        var ratio = Contrast.Ratio(palette.Get(foreground), palette.Get(background));

        Assert.True(ratio >= minimum, $"{look}: {foreground} on {background} is {ratio:F2} to 1, needs {minimum}.");
    }

    [Theory]
    [MemberData(nameof(Looks))]
    public void Every_slice_colour_stands_out_from_the_card(string look)
    {
        var card = Of(look).Card;

        Assert.All(SliceColors.All.Append(SliceColors.Uncategorized), color =>
            Assert.True(Contrast.Ratio(color, card) >= 3, $"{look}: slice {color} on the card is {Contrast.Ratio(color, card):F2} to 1."));
    }

    [Fact]
    public void Slice_colours_are_distinct_and_enough_for_every_built_in_spending_category()
    {
        var spending = CategorizationConfig.Default.Catalog.Categories
            .Where(c => c.Role == CategoryRole.Spending && !c.IsUncategorized)
            .ToList();

        Assert.Equal(SliceColors.All.Count, SliceColors.All.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.DoesNotContain(SliceColors.Uncategorized, SliceColors.All);
        Assert.True(SliceColors.All.Count >= spending.Count);
        var colors = spending.Select(c => SliceColors.For(CategorizationConfig.Default.Catalog, c.Id)).ToList();
        Assert.Equal(colors.Count, colors.Distinct().Count());
    }

    [Fact]
    public void Uncategorized_spending_has_the_neutral_colour()
    {
        Assert.Equal(SliceColors.Uncategorized, SliceColors.For(CategorizationConfig.Default.Catalog, new CategoryId("uncategorized-out")));
    }

    [Fact]
    public void A_category_has_the_same_slice_colour_in_every_statement()
    {
        var seen = new Dictionary<CategoryId, string>();
        foreach (var sample in Samples.Valid)
        {
            var view = new StatementViewModel(CategorizationConfig.Default);
            view.Show(Samples.Read(sample));
            foreach (var slice in view.Slices)
            {
                Assert.Equal(SliceColors.For(CategorizationConfig.Default.Catalog, slice.Category), slice.Color);
                if (seen.TryGetValue(slice.Category, out var earlier))
                    Assert.Equal(earlier, slice.Color);
                seen[slice.Category] = slice.Color;
            }
        }
        Assert.NotEmpty(seen);
    }

    [Fact]
    public void Every_colour_is_a_six_digit_hex_value()
    {
        var all = new[] { Palette.Light, Palette.Dark }.SelectMany(p => p.All().Select(c => c.Value))
            .Concat(SliceColors.All).Append(SliceColors.Uncategorized);

        Assert.All(all, c => Assert.Matches("^#[0-9A-F]{6}$", c));
    }
}

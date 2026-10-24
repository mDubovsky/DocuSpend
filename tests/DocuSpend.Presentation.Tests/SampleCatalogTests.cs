namespace DocuSpend.Presentation.Tests;

/// <summary>The valid samples travel with the application and can be chosen (spec: starting and choosing a sample).</summary>
public class SampleCatalogTests
{
    [Fact]
    public void The_valid_samples_are_offered_in_order_with_slovak_titles()
    {
        var samples = SampleCatalog.Available(Samples.Dir);

        Assert.Equal(Samples.Valid, samples.Select(s => s.Name));
        Assert.All(samples, s => Assert.False(string.IsNullOrWhiteSpace(s.Title)));
        Assert.Equal(samples.Count, samples.Select(s => s.Title).Distinct().Count());
        Assert.All(samples, s => Assert.True(File.Exists(s.Path)));
    }

    [Fact]
    public void Files_that_are_not_valid_statements_are_never_offered()
    {
        // samples/public also holds samples 06 to 09, which are not statements; the application shows only 01 to 05.
        Assert.True(File.Exists(Path.Combine(Samples.Dir, "06-other-bank-statement.pdf")));

        Assert.DoesNotContain(SampleCatalog.Available(Samples.Dir), s => s.Name.StartsWith("06", StringComparison.Ordinal));
    }

    [Fact]
    public void A_missing_folder_offers_nothing()
    {
        Assert.Empty(SampleCatalog.Available(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
    }

    [Fact]
    public void The_first_sample_is_the_one_shown_at_start()
    {
        Assert.Equal("01-typical-month", SampleCatalog.Available(Samples.Dir)[0].Name);
    }
}

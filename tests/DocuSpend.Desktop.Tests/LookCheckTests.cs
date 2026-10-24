namespace DocuSpend.Desktop.Tests;

/// <summary>Screens use only the shared look (spec: one look, defined once); the check is shown failing on a fixture.</summary>
public class LookCheckTests
{
    private const string Fixture = """
        <Window xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
          <Window.Styles>
            <Style Selector="TextBlock.label">
              <Setter Property="FontSize" Value="12" />
              <Setter Property="Foreground" Value="{DynamicResource Look.Muted}" />
            </Style>
          </Window.Styles>
          <StackPanel Spacing="8" Margin="{StaticResource Look.Margin.Card}" Background="Transparent">
            <TextBlock Text="a" Foreground="#5F6368" />
            <TextBlock Text="b" Foreground="Firebrick" FontSize="{StaticResource Look.FontSize.Label}" />
            <Border CornerRadius="6" BorderThickness="0" Width="120" Height="40" />
          </StackPanel>
        </Window>
        """;

    [Fact]
    public void Literal_colours_sizes_roundings_and_spacings_fail()
    {
        var violations = LookCheck.Violations("Fixture.axaml", Fixture);

        Assert.Equal(5, violations.Count);
        Assert.Contains(violations, v => v.Contains("FontSize=\"12\""));
        Assert.Contains(violations, v => v.Contains("Spacing=\"8\""));
        Assert.Contains(violations, v => v.Contains("Foreground=\"#5F6368\""));
        Assert.Contains(violations, v => v.Contains("Foreground=\"Firebrick\""));
        Assert.Contains(violations, v => v.Contains("CornerRadius=\"6\""));
        Assert.All(violations, v => Assert.Contains(LookCheck.LookFile, v));
    }

    [Fact]
    public void The_check_finds_the_screens_and_leaves_out_the_look()
    {
        var files = LookCheck.ScreenFiles();

        Assert.Contains("MainWindow.axaml", files);
        Assert.DoesNotContain(LookCheck.LookFile, files);
    }

    public static TheoryData<string> Screens => new(LookCheck.ScreenFiles());

    [Theory]
    [MemberData(nameof(Screens))]
    public void Every_screen_uses_only_the_shared_look(string file)
    {
        var violations = LookCheck.Violations(file, File.ReadAllText(LookCheck.PathOf(file)));

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }
}

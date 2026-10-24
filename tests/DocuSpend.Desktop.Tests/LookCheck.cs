using System.Xml.Linq;
using Avalonia.Media;

namespace DocuSpend.Desktop.Tests;

/// <summary>
/// The screens use only the shared look (spec: one look, defined once). In a screen's XAML, a colour, a text size, a
/// corner rounding or a spacing value must refer to a resource; only Look/Look.axaml defines them.
/// </summary>
public static class LookCheck
{
    /// <summary>The file that defines the look; the only one allowed literal values.</summary>
    public const string LookFile = "Look/Look.axaml";

    /// <summary>Properties that belong to the look. Layout that is structure, such as a column width, is not here.</summary>
    private static readonly HashSet<string> LookProperties =
    [
        "FontSize", "FontWeight", "CornerRadius", "Margin", "Padding", "Spacing", "RowSpacing", "ColumnSpacing",
        "Foreground", "Background", "BorderBrush", "BorderThickness", "BoxShadow", "Opacity",
    ];

    /// <summary>Values that aren't a choice of look.</summary>
    private static readonly HashSet<string> Neutral = ["Transparent", "0"];

    public static IReadOnlyList<string> Violations(string file, string xaml)
    {
        var violations = new List<string>();
        foreach (var element in XDocument.Parse(xaml).Descendants())
        {
            foreach (var attribute in element.Attributes().Where(a => !a.IsNamespaceDeclaration))
            {
                var name = attribute.Name.LocalName;
                var value = attribute.Value.Trim();
                if (value.StartsWith('{') || Neutral.Contains(value) || (element.Name.LocalName == "Setter" && name == "Value"))
                    continue;
                if (LookProperties.Contains(name) || Color.TryParse(value, out _))
                    violations.Add($"{file}: <{element.Name.LocalName} {name}=\"{value}\"> sets the look in a screen; use a resource from {LookFile}.");
            }
            // <Setter Property="FontSize" Value="16" /> is the same thing written as a style.
            if (element.Name.LocalName == "Setter" && (string?)element.Attribute("Property") is { } property
                && LookProperties.Contains(property) && (string?)element.Attribute("Value") is { } setterValue
                && !setterValue.StartsWith('{') && !Neutral.Contains(setterValue))
                violations.Add($"{file}: a style sets {property}=\"{setterValue}\" in a screen; use a resource from {LookFile}.");
        }
        return violations;
    }

    /// <summary>Every XAML file of the desktop application, except the look itself, as repository-relative paths.</summary>
    public static IReadOnlyList<string> ScreenFiles()
    {
        var root = RepoRoot();
        var app = Path.Combine(root, "src", "DocuSpend.Desktop");
        return Directory.EnumerateFiles(app, "*.axaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(f => Path.GetRelativePath(app, f).Replace('\\', '/'))
            .Where(f => f != LookFile)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    public static string PathOf(string screenFile) => Path.Combine(RepoRoot(), "src", "DocuSpend.Desktop", screenFile);

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DocuSpend.slnx")))
                return dir.FullName;
        }
        throw new InvalidOperationException("Repository root (DocuSpend.slnx) not found.");
    }
}

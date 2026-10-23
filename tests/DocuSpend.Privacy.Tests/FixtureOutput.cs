using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DocuSpend.Privacy.Tests;

/// <summary>
/// A throwaway build output directory filled with small assemblies compiled in the test, so the dependency check
/// can be shown to pass and fail on inputs whose contents are known exactly.
/// </summary>
public sealed class FixtureOutput : IDisposable
{
    private static readonly IReadOnlyList<MetadataReference> Runtime =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToList();

    private readonly Dictionary<string, byte[]> _images = [];

    public string Dir { get; } = Directory.CreateTempSubdirectory("docuspend-privacy-").FullName;

    /// <summary>Compiles an assembly into the output, referencing assemblies added before it by name.</summary>
    public byte[] Add(string name, string source, string[]? references = null, string version = "1.0.0.0")
    {
        var image = Compile(name, $"[assembly: System.Reflection.AssemblyVersion(\"{version}\")]\n{source}",
            (references ?? []).Select(r => MetadataReference.CreateFromImage(_images[r])));
        _images[name] = image;
        Write(name + ".dll", image);
        return image;
    }

    public void Write(string relativePath, byte[] content)
    {
        var path = Path.Combine(Dir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
    }

    public static byte[] Compile(string name, string source, IEnumerable<MetadataReference>? references = null)
    {
        var compilation = CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source)],
            Runtime.Concat(references ?? []), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
            throw new InvalidOperationException($"Fixture {name} doesn't compile: "
                + string.Join("; ", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        return stream.ToArray();
    }

    public void Dispose() => Directory.Delete(Dir, recursive: true);
}

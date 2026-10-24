using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace DocuSpend.Privacy.Tests;

/// <summary>What the metadata of one managed assembly says about it.</summary>
public sealed record InspectedAssembly(string Name, Version Version, IReadOnlyList<string> References,
    IReadOnlyList<string> NetworkingUses);

/// <summary>
/// Reads an assembly's metadata with System.Reflection.Metadata: nothing is loaded or run. Networking shows up
/// as a type it references, defines or forwards in a networking namespace (which also catches netstandard
/// libraries, whose references all point at netstandard), or as a P/Invoke into a networking library or function.
/// </summary>
public static class AssemblyInspector
{
    /// <summary>The assembly in a file, or null if the file isn't a managed assembly.</summary>
    public static InspectedAssembly? Inspect(byte[] image, NetworkingList networking)
    {
        try
        {
            using var pe = new PEReader(new MemoryStream(image, writable: false));
            if (!pe.HasMetadata)
                return null;
            var reader = pe.GetMetadataReader();
            if (!reader.IsAssembly)
                return null;
            return Inspect(reader, networking);
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    private static InspectedAssembly Inspect(MetadataReader reader, NetworkingList networking)
    {
        var definition = reader.GetAssemblyDefinition();
        var references = reader.AssemblyReferences
            .Select(h => reader.GetString(reader.GetAssemblyReference(h).Name))
            .ToList();

        var types = reader.TypeReferences.Select(h => OuterName(reader, reader.GetTypeReference(h)))
            .Concat(reader.TypeDefinitions.Select(h => reader.GetTypeDefinition(h))
                .Where(t => !t.IsNested)
                .Select(t => (reader.GetString(t.Namespace), reader.GetString(t.Name))))
            .Concat(reader.ExportedTypes.Select(h => reader.GetExportedType(h))
                .Select(t => (reader.GetString(t.Namespace), reader.GetString(t.Name))))
            .Where(t => networking.IsNetworkingType(t.Item1, t.Item2))
            .Select(t => $"{t.Item1}.{t.Item2}");

        var imports = reader.MethodDefinitions.Select(h => reader.GetMethodDefinition(h))
            .Where(m => (m.Attributes & MethodAttributes.PinvokeImpl) != 0)
            .Select(m => (Method: m, Import: m.GetImport()))
            .Select(x => (
                Library: reader.GetString(reader.GetModuleReference(x.Import.Module).Name),
                Function: x.Import.Name.IsNil ? reader.GetString(x.Method.Name) : reader.GetString(x.Import.Name)))
            .Where(x => networking.IsNetworkingImport(x.Library, x.Function))
            .Select(x => $"native {x.Library}!{x.Function}");

        var uses = types.Concat(imports).Distinct().Order(StringComparer.Ordinal).ToList();
        return new InspectedAssembly(reader.GetString(definition.Name), definition.Version, references, uses);
    }

    /// <summary>A type reference's namespace and name, or its outermost type's for a nested type.</summary>
    private static (string, string) OuterName(MetadataReader reader, TypeReference type)
    {
        while (type.ResolutionScope.Kind == HandleKind.TypeReference)
            type = reader.GetTypeReference((TypeReferenceHandle)type.ResolutionScope);
        return (reader.GetString(type.Namespace), reader.GetString(type.Name));
    }
}

using System.Reflection;
using DocuSpend.Statements.Reading;

namespace DocuSpend.Statements.Tests;

/// <summary>
/// The statement never leaves the machine: neither the library nor the assemblies it references use networking.
/// This checks references, not behavior; it catches a networking dependency being added, not every possible use.
/// </summary>
public class NoNetworkTests
{
    [Fact]
    public void Neither_the_reader_nor_its_dependencies_reference_networking_assemblies()
    {
        var seen = new HashSet<string>();
        var queue = new Queue<Assembly>([typeof(VubStatementReader).Assembly]);
        var offenders = new List<string>();

        while (queue.TryDequeue(out var assembly))
        {
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (!seen.Add(reference.Name!))
                    continue;
                if (reference.Name!.StartsWith("System.Net", StringComparison.Ordinal))
                    offenders.Add($"{assembly.GetName().Name} → {reference.Name}");
                // Follow only non-framework dependencies (the library and PdfPig); the runtime itself isn't in question.
                if (!reference.Name.StartsWith("System", StringComparison.Ordinal) && reference.Name != "netstandard")
                    queue.Enqueue(Assembly.Load(reference));
            }
        }

        Assert.Empty(offenders);
        Assert.Contains("UglyToad.PdfPig", seen);
    }
}

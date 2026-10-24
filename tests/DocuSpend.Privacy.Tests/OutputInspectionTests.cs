namespace DocuSpend.Privacy.Tests;

/// <summary>
/// A whole build output is inspected: networking anywhere in it is found with the path that leads to it, and native
/// files are listed (spec: privacy-dependencies).
/// </summary>
[Trait("Check", "privacy-dependencies")]
public class OutputInspectionTests
{
    private static OutputReport Inspect(FixtureOutput output, string root = "App") =>
        OutputInspection.Inspect(output.Dir, root, NetworkingList.Repository.Value);

    /// <summary>App → Toolkit → Net (uses HTTP); App → Clean; Orphan (uses sockets) is in the output but unreferenced.</summary>
    private static FixtureOutput Typical()
    {
        var output = new FixtureOutput();
        output.Add("Net", "public static class Net { public static object Get() => new System.Net.Http.HttpClient(); }", version: "2.1.0.0");
        output.Add("Toolkit", "public static class Toolkit { public static object Use() => Net.Get(); }", ["Net"]);
        output.Add("Clean", "public static class Clean { public static int Two() => 2; }");
        output.Add("Orphan", "public static class Orphan { public static object S() => new System.Net.Sockets.TcpClient(); }");
        output.Add("App", "public static class App { public static object Run() => (Toolkit.Use(), Clean.Two()); }", ["Toolkit", "Clean"]);
        return output;
    }

    [Fact]
    public void Networking_in_an_indirect_dependency_is_found_with_its_path()
    {
        using var output = Typical();

        var finding = Assert.Single(Inspect(output).Findings, f => f.Assembly == "Net");

        Assert.Equal(["App", "Toolkit", "Net"], finding.Path);
        Assert.Equal(new Version(2, 1, 0, 0), finding.Version);
        Assert.Equal("Net.dll", finding.File);
        Assert.Contains("System.Net.Http.HttpClient", finding.Uses);
        Assert.Equal("Net 2.1.0.0 (Net.dll) uses networking: System.Net.Http.HttpClient. Path: App → Toolkit → Net.", finding.ToString());
    }

    [Fact]
    public void Networking_in_an_assembly_nothing_references_is_still_found()
    {
        using var output = Typical();

        var finding = Assert.Single(Inspect(output).Findings, f => f.Assembly == "Orphan");

        Assert.Null(finding.Path);
        Assert.Contains("not referenced by anything", finding.ToString());
    }

    [Fact]
    public void Clean_assemblies_are_inspected_but_are_not_findings()
    {
        using var output = Typical();

        var report = Inspect(output);

        Assert.Equal(["App.dll", "Clean.dll", "Net.dll", "Orphan.dll", "Toolkit.dll"], report.ManagedFiles);
        Assert.Equal(["Net", "Orphan"], report.Findings.Select(f => f.Assembly).Order());
    }

    [Fact]
    public void An_assembly_importing_a_networking_library_is_found()
    {
        using var output = new FixtureOutput();
        output.Add("Importer", "public static class I { [System.Runtime.InteropServices.DllImport(\"ws2_32.dll\")] public static extern int WSACleanup(); }");
        output.Add("App", "public static class App { public static int Run() => I.WSACleanup(); }", ["Importer"]);

        var finding = Assert.Single(Inspect(output).Findings);

        Assert.Equal(["App", "Importer"], finding.Path);
        Assert.Equal(["native ws2_32.dll!WSACleanup"], finding.Uses);
    }

    [Fact]
    public void Native_files_and_unreadable_assemblies_are_listed_but_not_the_launcher()
    {
        using var output = new FixtureOutput();
        output.Add("App", "public static class App { }");
        output.Write("App.exe", [0x4D, 0x5A, 0, 0]);
        output.Write("runtimes/linux-x64/native/libfoo.so", [0x7F, (byte)'E', (byte)'L', (byte)'F']);
        output.Write("libbar.so.1", [0x7F, (byte)'E', (byte)'L', (byte)'F']);
        output.Write("Broken.dll", [1, 2, 3]);
        output.Write("App.deps.json", "{}"u8.ToArray());

        var report = Inspect(output);

        Assert.Equal(["Broken.dll", "libbar.so.1", "runtimes/linux-x64/native/libfoo.so"], report.NativeFiles);
        Assert.Equal(["App.dll"], report.ManagedFiles);
    }

    [Fact]
    public void A_missing_output_or_a_missing_root_assembly_fails_rather_than_passing_empty()
    {
        using var output = new FixtureOutput();
        output.Add("Other", "public class O { }");

        Assert.Throws<DirectoryNotFoundException>(() =>
            OutputInspection.Inspect(Path.Combine(output.Dir, "missing"), "App", NetworkingList.Repository.Value));
        Assert.Throws<InvalidOperationException>(() => Inspect(output));
    }
}

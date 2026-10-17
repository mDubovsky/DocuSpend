namespace DocuSpend.Statements.Reading;

/// <summary>Reads one bank's statement format. Choosing between readers for several banks is not built yet.</summary>
public interface IStatementReader
{
    /// <summary>Reads a whole statement file. Uses only the bytes given; no network.</summary>
    ReadResult Read(byte[] file);
}

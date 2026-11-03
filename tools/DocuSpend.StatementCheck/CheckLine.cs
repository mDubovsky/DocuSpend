using DocuSpend.Statements.Model;

namespace DocuSpend.StatementCheck;

/// <summary>What the check prints for a statement it read: counts only, never anything from the file.</summary>
internal static class CheckLine
{
    public static string Ok(string fileName, int pages, Statement statement) =>
        $"{fileName}: OK ({pages} pages, {statement.Transactions.Count} transactions, " +
        $"{statement.Transactions.Count(t => t.Type == TransactionType.Unknown)} of unknown type)";
}

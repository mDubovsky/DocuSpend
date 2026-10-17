using DocuSpend.Statements.Model;

namespace DocuSpend.Statements.Reading;

public enum RejectionKind
{
    /// <summary>The file has no bytes.</summary>
    EmptyFile,

    /// <summary>The file can't be opened as a PDF: damaged, cut off, protected or not a PDF at all.</summary>
    NotAPdf,

    /// <summary>A readable PDF that doesn't have the structure of a statement this reader knows.</summary>
    NotAVubStatement,

    /// <summary>A recognized statement with a part that can't be understood.</summary>
    Unreadable,

    /// <summary>A statement that was read but disagrees with its own printed totals.</summary>
    DoesNotReconcile,
}

/// <summary>
/// Why a file could not be read. The message is for people: it names the problem and where it is,
/// and never repeats the file's content (names, account numbers, amounts), since messages end up in logs.
/// </summary>
public sealed record Rejection(RejectionKind Kind, string Message);

/// <summary>Either a statement or a rejection, never both and never a partial statement.</summary>
public sealed class ReadResult
{
    private ReadResult(Statement? statement, Rejection? rejection)
    {
        Statement = statement;
        Rejection = rejection;
    }

    public Statement? Statement { get; }
    public Rejection? Rejection { get; }
    public bool IsSuccess => Statement is not null;

    public static ReadResult Success(Statement statement) => new(statement, null);

    public static ReadResult Reject(RejectionKind kind, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new ReadResult(null, new Rejection(kind, message));
    }
}

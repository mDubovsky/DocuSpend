using System.Security;

namespace DocuSpend.Presentation;

/// <summary>Why a chosen file isn't shown: one of these has its own message for the user (spec: the messages table).</summary>
public enum OpenProblem
{
    Empty,
    NotAPdf,
    NotAVubStatement,
    Unreadable,
    DoesNotReconcile,
    CannotOpen,
    TooLarge,
}

/// <summary>A file's bytes, or why it couldn't be read; never both.</summary>
public sealed record FileContent(byte[]? Bytes, OpenProblem? Problem);

/// <summary>
/// Reads the file the user chose into memory, and nothing else: no copy, no cache, nothing written (spec: nothing is
/// kept). Whether the bytes are a statement is the reader's answer, not this class's.
/// </summary>
public static class StatementFile
{
    /// <summary>Far beyond any monthly statement (well under 1 MB); a larger file isn't read at all.</summary>
    public const long MaxBytes = 50L * 1024 * 1024;

    /// <summary>Reads on a background thread, so the window stays responsive.</summary>
    public static Task<FileContent> ReadAsync(string path) => Task.Run(() => Read(path));

    private static FileContent Read(string path)
    {
        try
        {
            // Sharing as much as possible, so a statement open in a PDF viewer can still be read.
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (file.Length > MaxBytes)
                return new FileContent(null, OpenProblem.TooLarge);
            var bytes = new byte[file.Length];
            file.ReadExactly(bytes);
            return new FileContent(bytes, null);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException
                                       or NotSupportedException or SecurityException)
        {
            return new FileContent(null, OpenProblem.CannotOpen);
        }
    }
}

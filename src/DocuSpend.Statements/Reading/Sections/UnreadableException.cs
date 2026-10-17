namespace DocuSpend.Statements.Reading.Sections;

/// <summary>
/// Raised inside the reader when a recognized statement has a part it can't understand; turned into an
/// <see cref="RejectionKind.Unreadable"/> rejection. The message names the part, never the file's content.
/// </summary>
internal sealed class UnreadableException(string message) : Exception(message);

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DocuSpend.Statements.Model;

namespace DocuSpend.Categorization.Rules;

/// <summary>
/// What DocuSpend keeps to recognise a transaction the user categorized by hand: a keyed hash of its details, from
/// which the details can't be read back (work/022-categorize-from-list/spec.md: what is kept). 64 lower-case hex
/// characters.
/// </summary>
public readonly record struct Fingerprint
{
    public Fingerprint(string value)
    {
        if (!IsValid(value))
            throw new ArgumentException("A fingerprint is 64 lower-case hexadecimal characters.", nameof(value));
        Value = value;
    }

    public string Value { get; }

    public static bool IsValid(string? value) =>
        value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    public override string ToString() => Value;
}

/// <summary>
/// The key fingerprints are made with: 32 random bytes made on the user's computer the first time they categorize a
/// transaction by hand, and kept with the hand choices. Without it, the same transaction would give the same
/// fingerprint for everyone, so one table of guesses could test anyone's saved file (plan: choices to know about).
/// </summary>
public sealed class FingerprintKey
{
    public const int Length = 32;

    private readonly byte[] _bytes;

    private FingerprintKey(byte[] bytes) => _bytes = bytes;

    /// <summary>A new random key.</summary>
    public static FingerprintKey Create() => new(RandomNumberGenerator.GetBytes(Length));

    /// <summary>The key from its saved text, or null when the text isn't base64 of <see cref="Length"/> bytes.</summary>
    public static FingerprintKey? FromText(string? text)
    {
        if (text is null)
            return null;
        var bytes = new byte[Length + 3];
        return Convert.TryFromBase64String(text, bytes, out var written) && written == Length
            ? new FingerprintKey(bytes[..Length])
            : null;
    }

    /// <summary>The key as saved text (base64).</summary>
    public string ToText() => Convert.ToBase64String(_bytes);

    internal byte[] Hash(byte[] data) => HMACSHA256.HashData(_bytes, data);
}

/// <summary>Makes the fingerprints of a statement's transactions.</summary>
public static class Fingerprints
{
    /// <summary>Leads every hashed text, so a later recipe never matches this one by accident.</summary>
    private const string Recipe = "docuspend-fingerprint-1";

    /// <summary>
    /// One fingerprint per transaction, in the statement's order. Made from the booking date, the amount, the
    /// description and the counterparty's name and account, and the transaction's place among the earlier
    /// transactions of the statement that have all of those the same. Nothing about the file or the statement is used,
    /// so the same transaction downloaded again, or in an overlapping statement, gets the same fingerprint.
    /// </summary>
    public static IReadOnlyList<Fingerprint> Of(Statement statement, FingerprintKey key)
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new List<Fingerprint>(statement.Transactions.Count);
        var text = new StringBuilder();
        foreach (var transaction in statement.Transactions)
        {
            var details = Details(transaction);
            var place = seen[details] = seen.GetValueOrDefault(details) + 1;
            text.Clear().Append(details);
            Field(text, place.ToString(CultureInfo.InvariantCulture));
            var hash = key.Hash(Encoding.UTF8.GetBytes(text.ToString()));
            result.Add(new Fingerprint(Convert.ToHexStringLower(hash)));
        }
        return result;
    }

    /// <summary>
    /// The fields as one unambiguous text: each is prefixed with its length, and a missing field is written as "-",
    /// which no length is, so no two different transactions run together into the same text.
    /// </summary>
    private static string Details(Transaction t)
    {
        var text = new StringBuilder();
        Field(text, Recipe);
        Field(text, t.BookingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Field(text, t.Amount.ToString("0.00", CultureInfo.InvariantCulture));
        Field(text, t.Description);
        Field(text, t.CounterpartyName);
        Field(text, t.CounterpartyAccount);
        return text.ToString();
    }

    private static void Field(StringBuilder text, string? value)
    {
        if (value is null)
            text.Append("-;");
        else
            text.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append(';');
    }
}

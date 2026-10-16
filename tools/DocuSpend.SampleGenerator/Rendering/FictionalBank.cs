namespace DocuSpend.SampleGenerator.Rendering;

/// <summary>
/// The invented bank whose identity the samples carry. Samples reproduce VÚB's statement layout, but never VÚB's
/// identity (name, legal details, BIC, bank code, product names), so they can't pass for real VÚB documents.
/// See docs/decisions/0002-fictional-bank-identity-in-samples.md.
/// </summary>
public static class FictionalBank
{
    public const string Name = "Vzorová banka, a.s.";

    /// <summary>Not an assigned Slovak bank code.</summary>
    public const string BankCode = "9900";

    /// <summary>Not an assigned BIC.</summary>
    public const string Bic = "VZORSKXX";

    public const string ProductName = "Účet Štandard";

    /// <summary>The two-line legal identity printed at the top of every page, in place of the real bank's.</summary>
    public static readonly string[] LegalLines =
    [
        "Vzorová banka, a.s. (fiktívna), Príkladová 1, 000 00 Vzorovo, Obch. reg.: fiktívny,",
        "Oddiel: Sa, Vložka č. 0000/X, IČO: 00000000, www.vzorova-banka.example",
    ];

    /// <summary>Printed at the bottom of every page, in addition to the margin marking.</summary>
    public const string Footer = "SYNTHETIC TEST DATA – NOT A REAL STATEMENT · fictional bank · generated for DocuSpend tests";
}

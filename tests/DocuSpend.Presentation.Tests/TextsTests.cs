using DocuSpend.Categorization.Model;
using DocuSpend.Statements.Model;

namespace DocuSpend.Presentation.Tests;

/// <summary>Every name and message the interface shows exists in Slovak (decision 0003).</summary>
public class TextsTests
{
    public static TheoryData<TransactionType> Types => new(Enum.GetValues<TransactionType>());

    [Theory]
    [MemberData(nameof(Types))]
    public void Every_transaction_type_has_a_slovak_name(TransactionType type)
    {
        var name = Texts.TypeName(type);

        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.NotEqual(type.ToString(), name);
    }

    [Fact]
    public void Transaction_type_names_are_distinct()
    {
        var names = Enum.GetValues<TransactionType>().Select(Texts.TypeName).ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
    }

    public static TheoryData<RefusalKind> Refusals => new(Enum.GetValues<RefusalKind>());

    [Theory]
    [MemberData(nameof(Refusals))]
    public void Every_refusal_of_the_categorizer_has_a_slovak_message(RefusalKind kind)
    {
        // The categorizer's own messages are English, for developers; the interface shows these instead.
        var message = Texts.Refusal(kind);

        Assert.False(string.IsNullOrWhiteSpace(message));
        Assert.EndsWith(".", message);
    }
}

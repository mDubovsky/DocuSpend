namespace DocuSpend.SampleGenerator.Scenarios;

/// <summary>
/// SplitMix64. Used instead of System.Random, whose seeded sequence is not guaranteed
/// to stay the same across .NET versions; generated samples must not change silently.
/// </summary>
public sealed class SeededRandom(ulong seed)
{
    private ulong _state = seed;

    public ulong NextUInt64()
    {
        var z = _state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>A number in [0, maxExclusive).</summary>
    public int Next(int maxExclusive) => (int)(NextUInt64() % (ulong)maxExclusive);

    public int Next(int minInclusive, int maxExclusive) => minInclusive + Next(maxExclusive - minInclusive);

    public T Pick<T>(IReadOnlyList<T> items) => items[Next(items.Count)];

    /// <summary>A whole-cent amount in [min, max].</summary>
    public decimal Amount(decimal min, decimal max) =>
        min + Next((int)((max - min) * 100) + 1) / 100m;

    public string Digits(int count) =>
        string.Concat(Enumerable.Range(0, count).Select(_ => (char)('0' + Next(10))));
}

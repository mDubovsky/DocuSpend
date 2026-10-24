namespace DocuSpend.Presentation.Look;

/// <summary>Which look the user wants: Windows' own setting, or always light or always dark.</summary>
public enum LookChoice { System, Light, Dark }

public sealed record LookOption(LookChoice Choice, string Label);

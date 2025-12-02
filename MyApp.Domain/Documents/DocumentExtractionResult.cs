// PATH: MyApp.Domain/Documents/DocumentExtractionResult.cs
using System.Collections.Generic;

namespace MyApp.Domain.Documents;

public class ExtractedField
{
    public string Name { get; set; } = string.Empty;

    public string? Value { get; set; }

    public double Confidence { get; set; }
}

public class DocumentExtractionResult
{
    public bool Success { get; set; }

    public List<ExtractedField> Fields { get; set; } = new();

    public string? RawText { get; set; }

    public string? ErrorMessage { get; set; }
}

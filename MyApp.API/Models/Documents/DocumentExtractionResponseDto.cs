// PATH: MyApp.API/Models/Documents/DocumentExtractionResponseDto.cs
using System.Collections.Generic;

namespace MyApp.API.Models.Documents;

public class DocumentExtractionResponseDto
{
    public bool Success { get; set; }

    public List<ExtractedFieldDto> Fields { get; set; } = new();

    public string? RawText { get; set; }

    public string? ErrorMessage { get; set; }
}

public class ExtractedFieldDto
{
    public string Name { get; set; } = string.Empty;

    public string? Value { get; set; }

    public double Confidence { get; set; }
}

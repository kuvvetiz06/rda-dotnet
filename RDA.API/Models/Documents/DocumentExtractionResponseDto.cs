// PATH: RDA.API/Models/Documents/DocumentExtractionResponseDto.cs
using System.Collections.Generic;
using System.Linq;
using RDA.Domain.Documents;

namespace RDA.API.Models.Documents;

public class DocumentExtractionResponseDto
{
    public bool Success { get; set; }

    public List<ExtractedFieldDto> Fields { get; set; } = new();

    public string? RawText { get; set; }

    public string? ErrorMessage { get; set; }

    public static DocumentExtractionResponseDto FromDomain(DocumentExtractionResult result)
    {
        return new DocumentExtractionResponseDto
        {
            Success = result.Success,
            RawText = result.RawText,
            ErrorMessage = result.ErrorMessage,
            Fields = result.Fields.Select(f => new ExtractedFieldDto
            {
                Name = f.Name,
                Value = f.Value,
                Confidence = f.Confidence
            }).ToList()
        };
    }
}

public class ExtractedFieldDto
{
    public string Name { get; set; } = string.Empty;

    public string? Value { get; set; }

    public double Confidence { get; set; }
}

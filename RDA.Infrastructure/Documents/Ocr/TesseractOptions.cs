// PATH: RDA.Infrastructure/Documents/Ocr/TesseractOptions.cs
namespace RDA.Infrastructure.Documents.Ocr;

public class TesseractOptions
{
    public string TessDataPath { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;
}

// PATH: RDA.Infrastructure/Documents/Ocr/TesseractOcrService.cs
using Microsoft.Extensions.Logging;
using RDA.Application.Documents.Services;

namespace RDA.Infrastructure.Documents.Ocr;

public class TesseractOcrService : IOcrService
{
    private readonly ILogger<TesseractOcrService> _logger;

    public TesseractOcrService(ILogger<TesseractOcrService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextFromPdfAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        if (pdfStream == null)
        {
            throw new ArgumentNullException(nameof(pdfStream));
        }

        await using var memoryStream = new MemoryStream();
        await pdfStream.CopyToAsync(memoryStream, cancellationToken);

        if (memoryStream.Length == 0)
        {
            throw new InvalidOperationException("PDF stream is empty.");
        }

        // TODO: Gerçek senaryoda Pdf → Image → OCR pipeline eklenecek.
        _logger.LogInformation("Starting OCR extraction for PDF of size {SizeInBytes} bytes.", memoryStream.Length);

        // Demo amaçlı basit geri dönüş.
        var dummyText = "Dummy OCR text extracted from PDF for prototyping.";
        return dummyText;
    }
}

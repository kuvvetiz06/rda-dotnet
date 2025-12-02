// PATH: MyApp.Infrastructure/Documents/Ocr/TesseractOcrService.cs
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyApp.Application.Documents.Services;

namespace MyApp.Infrastructure.Documents.Ocr;

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

        try
        {
            using var memoryStream = new MemoryStream();
            await pdfStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            if (memoryStream.Length == 0)
            {
                throw new InvalidOperationException("PDF stream is empty.");
            }

            // TODO: Gerçek senaryoda Pdf → Image → OCR pipeline eklenecek
            _logger.LogInformation("OCR extraction invoked. TODO: avoid logging full text content for PII safety.");
            var dummyText = Encoding.UTF8.GetString(memoryStream.ToArray());
            return string.IsNullOrWhiteSpace(dummyText) ? "Dummy OCR text placeholder" : dummyText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR extraction failed. TODO: log only safe excerpts of the document text (first 100 chars).");
            throw;
        }
    }
}

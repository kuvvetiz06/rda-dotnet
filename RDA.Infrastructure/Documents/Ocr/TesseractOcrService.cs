// PATH: RDA.Infrastructure/Documents/Ocr/TesseractOcrService.cs
// NuGet Packages:
// - Tesseract
// - Magick.NET-Q8-AnyCPU
// - System.Drawing.Common (TODO: System.Drawing platform bağımlılığı için alternatif değerlendirilmesi gerekebilir.)
using System.Drawing;
using System.Text;
using ImageMagick;
using Microsoft.Extensions.Logging;
using RDA.Application.Documents.Services;
using Tesseract;

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
        if (pdfStream.CanSeek)
        {
            pdfStream.Position = 0;
        }

        await pdfStream.CopyToAsync(memoryStream, cancellationToken);

        if (memoryStream.Length == 0)
        {
            throw new InvalidOperationException("PDF stream is empty.");
        }

        try
        {
            memoryStream.Position = 0;

            var tessdataPath = "./tessdata";
            var turkishDataPath = Path.Combine(tessdataPath, "tur.traineddata");

            if (!File.Exists(turkishDataPath))
            {
                throw new FileNotFoundException("Turkish traineddata file is missing in tessdata directory.", turkishDataPath);
            }

            var readSettings = new MagickReadSettings
            {
                Density = new Density(300, 300)
            };

            using var images = new MagickImageCollection();
            images.Read(memoryStream, readSettings);

            if (images.Count == 0)
            {
                throw new InvalidOperationException("No pages found in PDF stream for OCR processing.");
            }

            var stringBuilder = new StringBuilder();
            _logger.LogInformation("Starting OCR extraction for PDF with {PageCount} pages.", images.Count);

            using var engine = new TesseractEngine(tessdataPath, "tur+eng", EngineMode.Default);

            for (var pageIndex = 0; pageIndex < images.Count; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var pageImage = images[pageIndex];
                using var bitmap = pageImage.ToBitmap();
                using var pix = PixConverter.ToPix(bitmap);
                using var page = engine.Process(pix, PageSegMode.Auto);

                var text = page.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    stringBuilder.AppendLine(text);
                }
            }

            return stringBuilder.ToString();
        }
        catch (MagickException ex)
        {
            _logger.LogError(ex, "Failed to convert PDF pages to images for OCR processing.");
            throw new InvalidOperationException("Image conversion from PDF failed. Ensure the PDF is valid and supported.", ex);
        }
        catch (TesseractException ex)
        {
            _logger.LogError(ex, "Tesseract OCR processing failed.");
            throw new InvalidOperationException("OCR processing failed due to Tesseract error.", ex);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException)
        {
            _logger.LogError(ex, "Unexpected error during OCR processing.");
            throw new InvalidOperationException("Unexpected error occurred during OCR processing.", ex);
        }
    }
}

// PATH: RDA.Infrastructure/Documents/Ocr/TesseractOcrService.cs
// NuGet: Magick.NET-Q8-AnyCPU, Tesseract
using System.Text;
using ImageMagick;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RDA.Application.Documents.Services;
using Tesseract;

namespace RDA.Infrastructure.Documents.Ocr;

public class TesseractOcrService : IOcrService
{
    private const int RenderDpi = 300;

    private readonly ILogger<TesseractOcrService> _logger;
    private readonly TesseractOptions _options;

    public TesseractOcrService(
        ILogger<TesseractOcrService> logger,
        IOptions<TesseractOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string> ExtractTextFromPdfAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);

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

        memoryStream.Position = 0;

        ValidateOptions();

        try
        {
            var readSettings = new MagickReadSettings
            {
                Density = new Density(RenderDpi, RenderDpi)
            };

            using var images = new MagickImageCollection();
            images.Read(memoryStream, readSettings);
            if (images.Count == 0)
            {
                throw new InvalidOperationException("No pages found in PDF stream for OCR processing.");
            }

            using var engine = new TesseractEngine(_options.TessDataPath, _options.Language, EngineMode.Default);
            var stringBuilder = new StringBuilder();

            for (var pageIndex = 0; pageIndex < images.Count; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var pageImage = (MagickImage)images[pageIndex].Clone();
                pageImage.ColorType = ColorType.Grayscale;
                pageImage.Format = MagickFormat.Png;

                await using var pageStream = new MemoryStream();
                await pageImage.WriteAsync(pageStream, cancellationToken);
                var pageBytes = pageStream.ToArray();

                using var pix = Pix.LoadFromMemory(pageBytes);
                using var page = engine.Process(pix, PageSegMode.Auto);

                var text = page.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    stringBuilder.AppendLine(text);
                }
            }

            return stringBuilder.ToString();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("OCR extraction was canceled.");
            throw;
        }
        catch (MagickException ex)
        {
            _logger.LogError(ex, "Failed to render PDF to images for OCR.");
            throw new InvalidOperationException("PDF rendering failed. Ensure the PDF is valid and supported.", ex);
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

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.TessDataPath))
        {
            throw new InvalidOperationException("Tesseract tessdata path is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Language))
        {
            throw new InvalidOperationException("Tesseract language is not configured.");
        }

        if (!Directory.Exists(_options.TessDataPath))
        {
            throw new DirectoryNotFoundException($"Tessdata directory not found at '{_options.TessDataPath}'.");
        }
    }
}

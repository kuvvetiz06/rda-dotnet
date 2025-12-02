// PATH: RDA.Infrastructure/Documents/Ocr/TesseractOcrService.cs
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfiumViewer;
using RDA.Application.Documents.Services;
using Tesseract;

namespace RDA.Infrastructure.Documents.Ocr;

public class TesseractOptions
{
    public string TessDataPath { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;
}

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
            using var document = PdfDocument.Load(memoryStream);
            var pageCount = document.PageCount;
            if (pageCount == 0)
            {
                throw new InvalidOperationException("No pages found in PDF stream for OCR processing.");
            }

            using var engine = new TesseractEngine(_options.TessDataPath, _options.Language, EngineMode.Default);
            var stringBuilder = new StringBuilder();

            for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var bitmap = RenderPageToGrayscale(document, pageIndex);
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
        catch (PdfDocumentException ex)
        {
            _logger.LogError(ex, "Failed to load PDF document for OCR processing.");
            throw new InvalidOperationException("PDF loading failed. Ensure the PDF is valid and supported.", ex);
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

    private static Bitmap RenderPageToGrayscale(PdfDocument document, int pageIndex)
    {
        var source = document.Render(pageIndex, RenderDpi, RenderDpi, true);
        try
        {
            var grayscale = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
            grayscale.SetResolution(RenderDpi, RenderDpi);

            using var graphics = Graphics.FromImage(grayscale);
            var colorMatrix = new ColorMatrix(
                new[]
                {
                    new[] {0.299f, 0.299f, 0.299f, 0f, 0f},
                    new[] {0.587f, 0.587f, 0.587f, 0f, 0f},
                    new[] {0.114f, 0.114f, 0.114f, 0f, 0f},
                    new[] {0f, 0f, 0f, 1f, 0f},
                    new[] {0f, 0f, 0f, 0f, 1f}
                });

            using var attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);

            graphics.DrawImage(
                source,
                new Rectangle(0, 0, source.Width, source.Height),
                0,
                0,
                source.Width,
                source.Height,
                GraphicsUnit.Pixel,
                attributes);

            return grayscale;
        }
        finally
        {
            source.Dispose();
        }
    }
}

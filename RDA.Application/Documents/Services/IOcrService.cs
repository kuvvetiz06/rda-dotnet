// PATH: RDA.Application/Documents/Services/IOcrService.cs
namespace RDA.Application.Documents.Services;

public interface IOcrService
{
    Task<string> ExtractTextFromPdfAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}

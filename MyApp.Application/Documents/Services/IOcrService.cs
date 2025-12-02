// PATH: MyApp.Application/Documents/Services/IOcrService.cs
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Application.Documents.Services;

public interface IOcrService
{
    Task<string> ExtractTextFromPdfAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}

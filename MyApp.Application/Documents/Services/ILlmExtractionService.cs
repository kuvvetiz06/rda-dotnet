// PATH: MyApp.Application/Documents/Services/ILlmExtractionService.cs
using System.Threading;
using System.Threading.Tasks;
using MyApp.Domain.Documents;

namespace MyApp.Application.Documents.Services;

public interface ILlmExtractionService
{
    Task<DocumentExtractionResult> ExtractFieldsAsync(string ocrText, string instructionPrompt, CancellationToken cancellationToken = default);
}

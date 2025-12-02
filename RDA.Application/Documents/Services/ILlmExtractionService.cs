// PATH: RDA.Application/Documents/Services/ILlmExtractionService.cs
using RDA.Domain.Documents;

namespace RDA.Application.Documents.Services;

public interface ILlmExtractionService
{
    Task<DocumentExtractionResult> ExtractFieldsAsync(string ocrText, string instructionPrompt, CancellationToken cancellationToken = default);
}

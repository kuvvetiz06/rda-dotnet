// PATH: MyApp.Application/Documents/Commands/ExtractFieldsFromPdf/ExtractFieldsFromPdfCommandHandler.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using MyApp.Application.Documents.Services;
using MyApp.Domain.Documents;

namespace MyApp.Application.Documents.Commands.ExtractFieldsFromPdf;

public class ExtractFieldsFromPdfCommandHandler : IRequestHandler<ExtractFieldsFromPdfCommand, DocumentExtractionResult>
{
    private readonly IOcrService _ocrService;
    private readonly ILlmExtractionService _llmExtractionService;
    private readonly ILogger<ExtractFieldsFromPdfCommandHandler> _logger;

    public ExtractFieldsFromPdfCommandHandler(
        IOcrService ocrService,
        ILlmExtractionService llmExtractionService,
        ILogger<ExtractFieldsFromPdfCommandHandler> logger)
    {
        _ocrService = ocrService;
        _llmExtractionService = llmExtractionService;
        _logger = logger;
    }

    public async Task<DocumentExtractionResult> Handle(ExtractFieldsFromPdfCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var ocrText = await _ocrService.ExtractTextFromPdfAsync(request.PdfStream, cancellationToken);

            if (string.IsNullOrWhiteSpace(ocrText))
            {
                _logger.LogWarning("OCR returned empty text for file {FileName}", request.FileName);
                return new DocumentExtractionResult
                {
                    Success = false,
                    ErrorMessage = "OCR did not return any text.",
                    RawText = null
                };
            }

            var extractionResult = await _llmExtractionService.ExtractFieldsAsync(ocrText, request.Prompt, cancellationToken);
            return extractionResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document extraction failed for file {FileName}. TODO: avoid logging full document text.", request.FileName);
            return new DocumentExtractionResult
            {
                Success = false,
                ErrorMessage = "Document extraction failed."
            };
        }
    }
}

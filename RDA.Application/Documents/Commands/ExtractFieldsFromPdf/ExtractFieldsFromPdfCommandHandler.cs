// PATH: RDA.Application/Documents/Commands/ExtractFieldsFromPdf/ExtractFieldsFromPdfCommandHandler.cs
using System.Linq;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RDA.Application.Documents.Services;
using RDA.Domain.Documents;

namespace RDA.Application.Documents.Commands.ExtractFieldsFromPdf;

public class ExtractFieldsFromPdfCommandHandler : IRequestHandler<ExtractFieldsFromPdfCommand, DocumentExtractionResult>
{
    private readonly IOcrService _ocrService;
    private readonly ILlmExtractionService _llmExtractionService;
    private readonly IValidator<ExtractFieldsFromPdfCommand> _validator;
    private readonly ILogger<ExtractFieldsFromPdfCommandHandler> _logger;

    public ExtractFieldsFromPdfCommandHandler(
        IOcrService ocrService,
        ILlmExtractionService llmExtractionService,
        IValidator<ExtractFieldsFromPdfCommand> validator,
        ILogger<ExtractFieldsFromPdfCommandHandler> logger)
    {
        _ocrService = ocrService;
        _llmExtractionService = llmExtractionService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<DocumentExtractionResult> Handle(ExtractFieldsFromPdfCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Document extraction validation failed: {Message}", errorMessage);

            return new DocumentExtractionResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        try
        {
            var ocrText = await _ocrService.ExtractTextFromPdfAsync(request.PdfStream, cancellationToken);
            var extractionResult = await _llmExtractionService.ExtractFieldsAsync(ocrText, request.Prompt, cancellationToken);
            return extractionResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document extraction failed for {FileName}", request.FileName);
            return new DocumentExtractionResult
            {
                Success = false,
                ErrorMessage = "Document extraction failed: " + ex.Message
            };
        }
    }
}

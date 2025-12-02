// PATH: RDA.Application/Documents/Commands/ExtractFieldsFromPdf/ExtractFieldsFromPdfCommand.cs
using MediatR;
using RDA.Domain.Documents;

namespace RDA.Application.Documents.Commands.ExtractFieldsFromPdf;

public record ExtractFieldsFromPdfCommand(Stream PdfStream, string FileName, string Prompt) : IRequest<DocumentExtractionResult>;

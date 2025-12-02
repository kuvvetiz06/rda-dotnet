// PATH: MyApp.Application/Documents/Commands/ExtractFieldsFromPdf/ExtractFieldsFromPdfCommand.cs
using System.IO;
using MediatR;
using MyApp.Domain.Documents;

namespace MyApp.Application.Documents.Commands.ExtractFieldsFromPdf;

public record ExtractFieldsFromPdfCommand(Stream PdfStream, string FileName, string Prompt) : IRequest<DocumentExtractionResult>;

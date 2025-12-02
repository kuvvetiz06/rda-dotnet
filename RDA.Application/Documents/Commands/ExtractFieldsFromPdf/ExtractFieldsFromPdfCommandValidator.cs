// PATH: RDA.Application/Documents/Commands/ExtractFieldsFromPdf/ExtractFieldsFromPdfCommandValidator.cs
using FluentValidation;

namespace RDA.Application.Documents.Commands.ExtractFieldsFromPdf;

public class ExtractFieldsFromPdfCommandValidator : AbstractValidator<ExtractFieldsFromPdfCommand>
{
    public ExtractFieldsFromPdfCommandValidator()
    {
        RuleFor(x => x.PdfStream)
            .NotNull().WithMessage("PDF stream cannot be null.")
            .Must(stream => stream.CanRead && stream.Length > 0)
            .WithMessage("PDF stream must be readable and non-empty.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .MaximumLength(255);

        RuleFor(x => x.Prompt)
            .NotEmpty().WithMessage("Prompt is required.")
            .MinimumLength(10)
            .MaximumLength(2000);
    }
}

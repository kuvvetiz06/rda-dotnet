// PATH: MyApp.Application/Documents/Commands/ExtractFieldsFromPdf/ExtractFieldsFromPdfCommandValidator.cs
using FluentValidation;

namespace MyApp.Application.Documents.Commands.ExtractFieldsFromPdf;

public class ExtractFieldsFromPdfCommandValidator : AbstractValidator<ExtractFieldsFromPdfCommand>
{
    public ExtractFieldsFromPdfCommandValidator()
    {
        RuleFor(x => x.PdfStream)
            .NotNull().WithMessage("PDF file stream must be provided.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .MaximumLength(255);

        RuleFor(x => x.Prompt)
            .NotEmpty().WithMessage("Prompt is required.")
            .MinimumLength(10)
            .MaximumLength(4000);
    }
}

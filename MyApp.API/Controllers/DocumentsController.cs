// PATH: MyApp.API/Controllers/DocumentsController.cs
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyApp.API.Models.Documents;
using MyApp.Application.Documents.Commands.ExtractFieldsFromPdf;

namespace MyApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DocumentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("extract-fields")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExtractFields([FromForm] IFormFile file, [FromForm] string prompt)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required.");
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            return BadRequest("Prompt is required.");
        }

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var command = new ExtractFieldsFromPdfCommand(memoryStream, file.FileName, prompt);
        var result = await _mediator.Send(command);

        var response = MapToDto(result);

        if (result.Success)
        {
            return Ok(response);
        }

        var statusCode = result.ErrorMessage?.Contains("validation", System.StringComparison.OrdinalIgnoreCase) == true
            ? StatusCodes.Status422UnprocessableEntity
            : StatusCodes.Status500InternalServerError;

        return StatusCode(statusCode, response);
    }

    private static DocumentExtractionResponseDto MapToDto(MyApp.Domain.Documents.DocumentExtractionResult result)
    {
        return new DocumentExtractionResponseDto
        {
            Success = result.Success,
            RawText = result.RawText,
            ErrorMessage = result.ErrorMessage,
            Fields = result.Fields
                .Select(f => new ExtractedFieldDto
                {
                    Name = f.Name,
                    Value = f.Value,
                    Confidence = f.Confidence
                })
                .ToList()
        };
    }
}

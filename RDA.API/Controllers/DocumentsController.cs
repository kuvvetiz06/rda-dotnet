// PATH: RDA.API/Controllers/DocumentsController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RDA.API.Models.Documents;
using RDA.Application.Documents.Commands.ExtractFieldsFromPdf;

namespace RDA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IMediator mediator, ILogger<DocumentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("extract-fields")]
    [ProducesResponseType(typeof(DocumentExtractionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DocumentExtractionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(DocumentExtractionResponseDto), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(DocumentExtractionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExtractFields([FromForm] IFormFile file, [FromForm] string prompt, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new DocumentExtractionResponseDto
            {
                Success = false,
                ErrorMessage = "PDF file is required."
            });
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            return BadRequest(new DocumentExtractionResponseDto
            {
                Success = false,
                ErrorMessage = "Prompt is required."
            });
        }

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var command = new ExtractFieldsFromPdfCommand(memoryStream, file.FileName, prompt);
        var result = await _mediator.Send(command, cancellationToken);

        var responseDto = DocumentExtractionResponseDto.FromDomain(result);
        if (result.Success)
        {
            return Ok(responseDto);
        }

        var statusCode = StatusCodes.Status422UnprocessableEntity;
        _logger.LogWarning("Document extraction failed for {FileName}: {Error}", file.FileName, result.ErrorMessage);
        return StatusCode(statusCode, responseDto);
    }
}

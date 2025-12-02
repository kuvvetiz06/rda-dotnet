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
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentExtractionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DocumentExtractionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(DocumentExtractionResponseDto), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(DocumentExtractionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExtractFields([FromForm] ExtractFieldsRequest request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(new DocumentExtractionResponseDto
            {
                Success = false,
                ErrorMessage = "PDF file is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new DocumentExtractionResponseDto
            {
                Success = false,
                ErrorMessage = "Prompt is required."
            });
        }

        await using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var command = new ExtractFieldsFromPdfCommand(memoryStream, request.File.FileName, request.Prompt);
        var result = await _mediator.Send(command, cancellationToken);

        var responseDto = DocumentExtractionResponseDto.FromDomain(result);
        if (result.Success)
        {
            return Ok(responseDto);
        }

        var statusCode = StatusCodes.Status422UnprocessableEntity;
        _logger.LogWarning("Document extraction failed for {FileName}: {Error}", request.File.FileName, result.ErrorMessage);
        return StatusCode(statusCode, responseDto);
    }
}

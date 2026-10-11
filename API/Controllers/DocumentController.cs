// Backend purpose: Exposes HTTP endpoints for the DocumentController backend feature.
using BookingSystem.Application.Commands.DocumentCommand.CreateDocument;
using BookingSystem.Application.Commands.DocumentCommand.DeleteDocument;
using BookingSystem.Application.Commands.DocumentCommand.UpdateDocument;
using BookingSystem.Application.Authorization;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Queries.QueriesDocument.GetAllDocuments;
using BookingSystem.Application.Queries.QueriesDocument.GetDocumentById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace API.Controllers;


[ApiController]
[Authorize(Policy = AccessPolicies.OnlyAdmin)]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AuditLogWriter _auditLogWriter;

    public DocumentController(IMediator mediator, AuditLogWriter auditLogWriter)
    {
        _mediator = mediator;
        _auditLogWriter = auditLogWriter;
    }

    // POST: api/document
    [HttpPost]
    public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentDto dto)
    {
        var result = await _mediator.Send(new CreateDocumentCommand(dto));
        if (!result.Success) return BadRequest(result.ErrorMessage);
        
        return CreatedAtAction(nameof(GetDocumentById), new { id = result.Data!.DocumentId }, result.Data);
        
    }

    // GET: api/document/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocumentById(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDocumentByIdQuery(id));
        if (result is null)
            return NotFound();
        if (!result.Success)
            return NotFound(result.ErrorMessage);
        if (result.Data is null)
            return NotFound();

        await _auditLogWriter.RecordReadAsync("Document", id.ToString(), cancellationToken);
        return Ok(result.Data);
    }

    // GET: api/document
    [HttpGet]
    public async Task<IActionResult> GetAllDocuments(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllDocumentsQuery());
        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        await _auditLogWriter.RecordReadAsync("Document", "list", cancellationToken);
        return Ok(result.Data);
    }

    // PUT: api/document/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocument(int id, [FromBody] CreateDocumentDto dto)
    {
        var result = await _mediator.Send(new UpdateDocumentCommand(id, dto));
        if (!result.Success)
            return NotFound(result.ErrorMessage);
        
        return NoContent();
    }

    // DELETE: api/document/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var result = await _mediator.Send(new DeleteDocumentCommand(id));
        if (!result.Success) return NotFound();

        return Ok(new { message = result.Message });
    }
}

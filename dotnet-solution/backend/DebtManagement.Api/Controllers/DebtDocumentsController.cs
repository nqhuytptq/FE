using System.Security.Claims;
using DebtManagement.Application.DTOs;
using DebtManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DebtManagement.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DebtDocumentsController(IDebtDocumentService service) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException());

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] Guid? departmentId, [FromQuery] string? search)
        => Ok(await service.GetAllAsync(status, departmentId, search, CurrentUserId));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var doc = await service.GetByIdAsync(id, CurrentUserId);
            return doc == null ? NotFound() : Ok(doc);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDebtDocumentRequest request)
    {
        var doc = await service.CreateAsync(request, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = doc.Id }, doc);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDebtDocumentRequest request)
    {
        try { return Ok(await service.UpdateAsync(id, request, CurrentUserId)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) { await service.DeleteAsync(id); return NoContent(); }

    [HttpPost("{id:guid}/pkh-sup-approve")]
    public async Task<IActionResult> PkhSupApprove(Guid id, [FromBody] ActionWithNotesRequest request)
    { try { await service.PkhSupApproveAsync(id, request?.Notes, CurrentUserId); return NoContent(); } catch (Exception ex) { return BadRequest(new { error = ex.Message }); } }

    [HttpPost("{id:guid}/pqln-receive")]
    public async Task<IActionResult> PqlnReceive(Guid id)
    { try { await service.PqlnTakeOwnershipAsync(id, CurrentUserId); return NoContent(); } catch (Exception ex) { return BadRequest(new { error = ex.Message }); } }

    [HttpPost("{id:guid}/pqln-submit-sup")]
    public async Task<IActionResult> PqlnSubmitSup(Guid id, [FromBody] ActionWithNotesRequest request)
    { try { await service.PqlnSubmitSupAsync(id, request?.Notes, CurrentUserId); return NoContent(); } catch (Exception ex) { return BadRequest(new { error = ex.Message }); } }

    [HttpPost("{id:guid}/pqln-sup-approve")]
    public async Task<IActionResult> PqlnSupApprove(Guid id, [FromBody] ActionWithNotesRequest request)
    { try { await service.PqlnSupApproveAsync(id, request?.Notes, CurrentUserId); return NoContent(); } catch (Exception ex) { return BadRequest(new { error = ex.Message }); } }

    [HttpPost("{id:guid}/pdv-receive")]
    public async Task<IActionResult> PdvReceive(Guid id)
    { try { await service.PdvTakeOwnershipAsync(id, CurrentUserId); return NoContent(); } catch (Exception ex) { return BadRequest(new { error = ex.Message }); } }

    [HttpPost("{id:guid}/pdv-post-entry")]
    public async Task<IActionResult> PdvPostEntry(Guid id, [FromBody] ActionWithNotesRequest request)
    { try { await service.PdvPostEntryAsync(id, request?.Notes, CurrentUserId); return NoContent(); } catch (Exception ex) { return BadRequest(new { error = ex.Message }); } }

    [HttpPost("{id:guid}/pdv-sup-approve")]
    public async Task<IActionResult> PdvSupApprove(Guid id, [FromBody] ActionWithNotesRequest request)
    { try { await service.PdvSupApproveAsync(id, request?.Notes, CurrentUserId); return NoContent(); } catch (Exception ex) { return BadRequest(new { error = ex.Message }); } }

    [HttpPost("{id:guid}/supplement")]
    public async Task<IActionResult> Supplement(Guid id, [FromBody] SupplementItemsRequest request)
    {
        try { await service.SupplementItemsAsync(id, request, CurrentUserId); return NoContent(); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/debt-items")]
    public async Task<IActionResult> AddDebtItems(Guid id, [FromBody] AddDebtItemsRequest request)
    {
        try { await service.AddDebtItemsAsync(id, request, CurrentUserId); return NoContent(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/return")]
    public async Task<IActionResult> Return(Guid id, [FromBody] ReturnDocumentRequest request)
    { try { await service.ReturnAsync(id, request.ToDepartmentId, request.Notes, CurrentUserId); return NoContent(); } catch (Exception ex) { return BadRequest(new { error = ex.Message }); } }
}

public record ReturnDocumentRequest(Guid ToDepartmentId, string? Notes);


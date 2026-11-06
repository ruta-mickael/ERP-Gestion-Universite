namespace erp_univ_back.Features.Evaluation.SaisirNotes;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/evaluations/{id:guid}/notes")]
public class SaisirNotesController(ISaisirNotesService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SaisirNotesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Saisir(
        [FromRoute] Guid id,
        [FromBody] SaisirNotesRequest request,
        CancellationToken ct)
    {
        var response = await service.ExecuteAsync(id, request, ct);
        return Ok(response);
    }
}

namespace erp_univ_back.Features.Evaluation.GetReleveNotes;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/evaluations/releve/{inscriptionId:guid}")]
public class GetReleveNotesController(IGetReleveNotesService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GetReleveNotesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenirReleve([FromRoute] Guid inscriptionId, CancellationToken ct)
    {
        var response = await service.ExecuteAsync(inscriptionId, ct);
        return Ok(response);
    }
}

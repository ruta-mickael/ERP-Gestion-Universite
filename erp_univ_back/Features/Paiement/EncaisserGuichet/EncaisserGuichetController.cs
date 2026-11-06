namespace erp_univ_back.Features.Paiement.EncaisserGuichet;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/paiements/guichet")]
public class EncaisserGuichetController(IEncaisserGuichetService service) : ControllerBase
{
    [HttpGet("situation/{matricule}")]
    [ProducesResponseType(typeof(SituationFinanciereEtudiantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenirSituation([FromRoute] string matricule, CancellationToken ct)
    {
        var response = await service.ObtenirSituationParMatriculeAsync(matricule, ct);
        return Ok(response);
    }

    [HttpPost("encaisser")]
    [ProducesResponseType(typeof(EncaisserGuichetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Encaisser([FromBody] EncaisserGuichetRequest request, CancellationToken ct)
    {
        var response = await service.ExecuteAsync(request, ct);
        return Ok(response);
    }
}

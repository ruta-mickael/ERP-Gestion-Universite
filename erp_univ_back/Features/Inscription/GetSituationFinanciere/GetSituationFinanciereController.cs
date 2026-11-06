namespace erp_univ_back.Features.Inscription.GetSituationFinanciere;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/inscriptions/situation-financiere")]
public class GetSituationFinanciereController(IGetSituationFinanciereService service) : ControllerBase
{
    [HttpGet("{matricule}")]
    [HttpGet("/api/paiements/guichet/situation/{matricule}")]
    [ProducesResponseType(typeof(SituationFinanciereEtudiantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenirSituation([FromRoute] string matricule, CancellationToken ct)
    {
        var response = await service.ExecuteAsync(matricule, ct);
        return Ok(response);
    }
}

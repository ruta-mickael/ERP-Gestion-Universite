namespace erp_univ_back.Features.Paiement.GetById;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/paiements")]
public class GetPaiementByIdController(IGetPaiementByIdService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetPaiementByIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenirParId([FromRoute] Guid id, CancellationToken ct)
    {
        var response = await service.ExecuteAsync(id, ct);
        return Ok(response);
    }
}

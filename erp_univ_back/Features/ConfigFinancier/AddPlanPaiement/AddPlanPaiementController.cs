namespace erp_univ_back.Features.ConfigFinancier.AddPlanPaiement;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/plans-paiement")]
public class AddPlanPaiementController(IAddPlanPaiementService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AddPlanPaiementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Creer([FromBody] AddPlanPaiementRequest request, CancellationToken ct)
    {
        var response = await service.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(Creer), new { id = response.Id }, response);
    }
}


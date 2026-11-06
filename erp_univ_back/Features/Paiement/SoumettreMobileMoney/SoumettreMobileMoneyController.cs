namespace erp_univ_back.Features.Paiement.SoumettreMobileMoney;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/paiements/externe")]
public class SoumettreMobileMoneyController(ISoumettreMobileMoneyService service) : ControllerBase
{
    [HttpPost("soumettre")]
    [ProducesResponseType(typeof(SoumettreMobileMoneyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Soumettre([FromBody] SoumettreMobileMoneyRequest request, CancellationToken ct)
    {
        var response = await service.ExecuteAsync(request, ct);
        return Ok(response);
    }
}


namespace erp_univ_back.Features.Paiement.ValiderTransaction;

using Microsoft.AspNetCore.Mvc;

public record RejeterTransactionInput(string MotifRejet);
public record ValiderTransactionInput(Guid? EcheanceCibleId = null);

[ApiController]
[Route("api/paiements")]
public class ValiderTransactionController(IValiderTransactionService service) : ControllerBase
{
    [HttpPost("{id:guid}/valider")]
    [ProducesResponseType(typeof(ValiderTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Valider(
        [FromRoute] Guid id,
        [FromBody] ValiderTransactionInput? input,
        CancellationToken ct)
    {
        var request = new ValiderTransactionRequest(
            EstValide: true,
            MotifRejet: null,
            EcheanceCibleId: input?.EcheanceCibleId
        );
        var response = await service.ExecuteAsync(id, request, ct);
        return Ok(response);
    }

    [HttpPost("{id:guid}/rejeter")]
    [ProducesResponseType(typeof(ValiderTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rejeter(
        [FromRoute] Guid id,
        [FromBody] RejeterTransactionInput input,
        CancellationToken ct)
    {
        var request = new ValiderTransactionRequest(
            EstValide: false,
            MotifRejet: input.MotifRejet,
            EcheanceCibleId: null
        );
        var response = await service.ExecuteAsync(id, request, ct);
        return Ok(response);
    }
}

namespace erp_univ_back.Features.Evaluation.Create;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/evaluations")]
public class CreateEvaluationController(ICreateEvaluationService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateEvaluationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Creer([FromBody] CreateEvaluationRequest request, CancellationToken ct)
    {
        var response = await service.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(Creer), new { id = response.Id }, response);
    }
}

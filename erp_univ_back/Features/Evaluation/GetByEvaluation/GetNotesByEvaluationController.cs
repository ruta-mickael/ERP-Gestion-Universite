namespace erp_univ_back.Features.Evaluation.GetByEvaluation;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/evaluations/{id:guid}/notes")]
public class GetNotesByEvaluationController(IGetNotesByEvaluationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GetNotesByEvaluationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenirParEvaluation([FromRoute] Guid id, CancellationToken ct)
    {
        var response = await service.ExecuteAsync(id, ct);
        return Ok(response);
    }
}

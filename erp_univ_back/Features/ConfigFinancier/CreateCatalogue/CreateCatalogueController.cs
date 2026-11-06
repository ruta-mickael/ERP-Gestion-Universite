namespace erp_univ_back.Features.ConfigFinancier.CreateCatalogue;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/frais-catalogue")]
public class CreateCatalogueController(ICreateCatalogueService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateCatalogueResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Creer([FromBody] CreateCatalogueRequest request, CancellationToken ct)
    {
        var response = await service.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(Creer), new { id = response.Id }, response);
    }
}


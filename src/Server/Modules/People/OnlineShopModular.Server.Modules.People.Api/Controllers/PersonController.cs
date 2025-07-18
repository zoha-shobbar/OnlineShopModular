using Microsoft.AspNetCore.Mvc;
using OnlineShopModular.Server.Modules.People.Infrastructure.Services;

namespace OnlineShopModular.Server.Modules.People.Api.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class PersonController : ControllerBase
{
    private readonly PersonService _personService;

    public PersonController(PersonService personService)
    {
        _personService = personService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PersonRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _personService.CreateAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _personService.GetByIdAsync(id, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }
}

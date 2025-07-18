using Microsoft.AspNetCore.Mvc;
using OnlineShopModular.Server.Modules.Library.Infrastructure.Services;

namespace OnlineShopModular.Server.Modules.Library.Api.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class BookController : ControllerBase
{
    private readonly BookService _bookService;

    public BookController(BookService bookService)
    {
        _bookService = bookService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookRequestDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _bookService.CreateAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var result = _bookService.GetById(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> LendBook([FromQuery] Guid personId, [FromQuery] Guid bookId, CancellationToken cancellationToken)
    {
        try
        {
            await _bookService.LendBookAsync(personId, bookId, cancellationToken);
            return Ok("کتاب با موفقیت امانت داده شد.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

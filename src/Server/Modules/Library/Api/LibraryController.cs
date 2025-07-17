using Microsoft.AspNetCore.Mvc;
using OnlineShopModular.Server.Modules.Library.Application;
using OnlineShopModular.Shared.Dtos.Library;

namespace OnlineShopModular.Server.Modules.Library.Api
{
    [ApiController]
    [Route("api/library")]
    public class LibraryController : ControllerBase
    {
        private readonly LibraryService _libraryService;

        public LibraryController(LibraryService libraryService)
        {
            _libraryService = libraryService;
        }

        [HttpPost("books")]
        public async Task<IActionResult> CreateBook([FromBody] BookRequestDto request)
        {
            var book = await _libraryService.CreateBook(request);
            return CreatedAtAction(nameof(GetBookById), new { id = book.Id }, book);
        }

        [HttpGet("books/{id}")]
        public async Task<IActionResult> GetBookById(Guid id)
        {
            var book = await _libraryService.GetBookById(id);
            if (book == null)
            {
                return NotFound();
            }
            return Ok(book);
        }

        [HttpPost("lend")]
        public async Task<IActionResult> LendBook([FromBody] LendBookRequestDto request)
        {
            var result = await _libraryService.LendBook(request);
            if (result == null)
            {
                return BadRequest("Book is not available.");
            }
            return Ok(result);
        }
    }
}

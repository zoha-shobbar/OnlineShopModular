
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

        [HttpGet("books/{id}")]
        public async Task<IActionResult> GetBookById(int id)
        {
            var result = await _libraryService.GetBookById(id);
            return Ok(result);
        }

        [HttpPost("books")]
        public async Task<IActionResult> CreateBook(RequestDto request)
        {
            var result = await _libraryService.CreateBook(request);
            return Ok(result);
        }

        [HttpPost("lend")]
        public async Task<IActionResult> LendBook(RequestDto request)
        {
            var result = await _libraryService.LendBook(request);
            return Ok(result);
        }
    }
}

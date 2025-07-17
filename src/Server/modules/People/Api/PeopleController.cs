
namespace OnlineShopModular.Server.Modules.People.Api
{
    [ApiController]
    [Route("api/people")]
    public class PeopleController : ControllerBase
    {
        private readonly PersonService _personService;

        public PeopleController(PersonService personService)
        {
            _personService = personService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _personService.GetPersonById(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(RequestDto request)
        {
            var result = await _personService.CreatePerson(request);
            return Ok(result);
        }
    }
}

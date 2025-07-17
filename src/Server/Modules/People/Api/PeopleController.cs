using Microsoft.AspNetCore.Mvc;
using OnlineShopModular.Server.Modules.People.Application;
using OnlineShopModular.Shared.Dtos.People;

namespace OnlineShopModular.Server.Modules.People.Api
{
    [ApiController]
    [Route("api/people")]
    public class PeopleController : ControllerBase
    {
        private readonly PeopleService _peopleService;

        public PeopleController(PeopleService peopleService)
        {
            _peopleService = peopleService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePerson([FromBody] PersonRequestDto request)
        {
            var person = await _peopleService.CreatePerson(request);
            return CreatedAtAction(nameof(GetPersonById), new { id = person.Id }, person);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPersonById(Guid id)
        {
            var person = await _peopleService.GetPersonById(id);
            if (person == null)
            {
                return NotFound();
            }
            return Ok(person);
        }
    }
}

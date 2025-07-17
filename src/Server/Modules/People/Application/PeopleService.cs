using OnlineShopModular.Server.Modules.People.Domain;
using OnlineShopModular.Shared.Dtos.People;

namespace OnlineShopModular.Server.Modules.People.Application
{
    public class PeopleService
    {
        private readonly IPersonRepository _personRepository;

        public PeopleService(IPersonRepository personRepository)
        {
            _personRepository = personRepository;
        }

        public async Task<PersonResponseDto> CreatePerson(PersonRequestDto request)
        {
            var person = new Person
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            await _personRepository.Create(person);

            return new PersonResponseDto
            {
                Id = person.Id,
                FirstName = person.FirstName,
                LastName = person.LastName
            };
        }

        public async Task<PersonResponseDto> GetPersonById(Guid id)
        {
            var person = await _personRepository.GetById(id);
            if (person == null)
            {
                return null;
            }

            return new PersonResponseDto
            {
                Id = person.Id,
                FirstName = person.FirstName,
                LastName = person.LastName
            };
        }
    }
}

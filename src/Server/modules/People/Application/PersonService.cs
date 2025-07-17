
namespace OnlineShopModular.Server.Modules.People.Application
{
    public class PersonService
    {
        private readonly IPersonRepository _personRepository;

        public PersonService(IPersonRepository personRepository)
        {
            _personRepository = personRepository;
        }

        public async Task<ResponseDto> GetPersonById(int id)
        {
            var person = await _personRepository.GetById(id);
            // In a real application, you would map the entity to a DTO.
            // For simplicity, we are returning the entity directly.
            return new ResponseDto();
        }

        public async Task<ResponseDto> CreatePerson(RequestDto request)
        {
            // In a real application, you would map the DTO to an entity.
            var person = new Person { FirstName = "New", LastName = "Person" };
            await _personRepository.Create(person);
            return new ResponseDto();
        }
    }
}

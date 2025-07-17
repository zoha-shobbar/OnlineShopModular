using Microsoft.EntityFrameworkCore;
using OnlineShopModular.Server.Modules.People.Application;
using OnlineShopModular.Server.Modules.People.Domain;

namespace OnlineShopModular.Server.Modules.People.Infrastructure
{
    public class PersonRepository : IPersonRepository
    {
        private readonly PeopleDbContext _context;

        public PersonRepository(PeopleDbContext context)
        {
            _context = context;
        }

        public async Task Create(Person person)
        {
            await _context.People.AddAsync(person);
            await _context.SaveChangesAsync();
        }

        public async Task<Person> GetById(Guid id)
        {
            return await _context.People.FindAsync(id);
        }
    }
}

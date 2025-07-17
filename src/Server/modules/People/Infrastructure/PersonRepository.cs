
namespace OnlineShopModular.Server.Modules.People.Infrastructure
{
    public class PeopleDbContext : DbContext
    {
        public DbSet<Person> People { get; set; }

        public PeopleDbContext(DbContextOptions<PeopleDbContext> options) : base(options)
        {
        }
    }

    public class PersonRepository : IPersonRepository
    {
        private readonly PeopleDbContext _context;

        public PersonRepository(PeopleDbContext context)
        {
            _context = context;
        }

        public async Task Create(Person person)
        {
            _context.People.Add(person);
            await _context.SaveChangesAsync();
        }

        public async Task<Person> GetById(int id)
        {
            return await _context.People.FindAsync(id);
        }
    }
}

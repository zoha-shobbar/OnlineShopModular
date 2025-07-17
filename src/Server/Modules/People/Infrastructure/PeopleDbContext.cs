using Microsoft.EntityFrameworkCore;
using OnlineShopModular.Server.Modules.People.Domain;

namespace OnlineShopModular.Server.Modules.People.Infrastructure
{
    public class PeopleDbContext : DbContext
    {
        public PeopleDbContext(DbContextOptions<PeopleDbContext> options) : base(options)
        {
        }

        public DbSet<Person> People { get; set; }
    }
}

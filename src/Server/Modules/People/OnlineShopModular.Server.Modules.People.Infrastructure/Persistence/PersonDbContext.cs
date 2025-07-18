
using Microsoft.EntityFrameworkCore;
using OnlineShopModular.Server.Modules.People.Domain.Entities;

namespace OnlineShopModular.Server.Modules.People.Infrastructure.Persistence;

public class PersonDbContext : DbContext
{
    public PersonDbContext(DbContextOptions<PersonDbContext> options) : base(options) { }

    public DbSet<Person> People => Set<Person>();
}

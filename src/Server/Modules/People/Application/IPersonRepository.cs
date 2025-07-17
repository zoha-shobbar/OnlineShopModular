using OnlineShopModular.Server.Modules.People.Domain;

namespace OnlineShopModular.Server.Modules.People.Application
{
    public interface IPersonRepository
    {
        Task<Person> GetById(Guid id);
        Task Create(Person person);
    }
}

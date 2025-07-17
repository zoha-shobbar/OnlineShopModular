
namespace OnlineShopModular.Server.Modules.People.Domain
{
    public interface IPersonRepository
    {
        Task<Person> GetById(int id);
        Task Create(Person person);
    }
}

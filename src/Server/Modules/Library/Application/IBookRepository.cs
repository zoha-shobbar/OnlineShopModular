using OnlineShopModular.Server.Modules.Library.Domain;

namespace OnlineShopModular.Server.Modules.Library.Application
{
    public interface IBookRepository
    {
        Task<Book> GetById(Guid id);
        Task Create(Book book);
        Task Update(Book book);
    }
}

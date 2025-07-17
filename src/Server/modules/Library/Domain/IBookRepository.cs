
namespace OnlineShopModular.Server.Modules.Library.Domain
{
    public interface IBookRepository
    {
        Task<Book> GetById(int id);
        Task Create(Book book);
    }
}

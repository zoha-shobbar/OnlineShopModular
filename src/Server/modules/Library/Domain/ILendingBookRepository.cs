
namespace OnlineShopModular.Server.Modules.Library.Domain
{
    public interface ILendingBookRepository
    {
        Task<LendingBook> GetById(int id);
        Task Create(LendingBook lendingBook);
    }
}

using OnlineShopModular.Server.Modules.Library.Domain;

namespace OnlineShopModular.Server.Modules.Library.Application
{
    public interface ILendingBookRepository
    {
        Task<LendingBook> GetById(Guid id);
        Task Create(LendingBook lendingBook);
    }
}

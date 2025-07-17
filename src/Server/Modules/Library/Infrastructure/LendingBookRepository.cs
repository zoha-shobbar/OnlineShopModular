using Microsoft.EntityFrameworkCore;
using OnlineShopModular.Server.Modules.Library.Application;
using OnlineShopModular.Server.Modules.Library.Domain;

namespace OnlineShopModular.Server.Modules.Library.Infrastructure
{
    public class LendingBookRepository : ILendingBookRepository
    {
        private readonly LibraryDbContext _context;

        public LendingBookRepository(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task Create(LendingBook lendingBook)
        {
            await _context.LendingBooks.AddAsync(lendingBook);
            await _context.SaveChangesAsync();
        }

        public async Task<LendingBook> GetById(Guid id)
        {
            return await _context.LendingBooks.FindAsync(id);
        }
    }
}

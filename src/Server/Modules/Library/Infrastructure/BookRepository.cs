using Microsoft.EntityFrameworkCore;
using OnlineShopModular.Server.Modules.Library.Application;
using OnlineShopModular.Server.Modules.Library.Domain;

namespace OnlineShopModular.Server.Modules.Library.Infrastructure
{
    public class BookRepository : IBookRepository
    {
        private readonly LibraryDbContext _context;

        public BookRepository(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task Create(Book book)
        {
            await _context.Books.AddAsync(book);
            await _context.SaveChangesAsync();
        }

        public async Task<Book> GetById(Guid id)
        {
            return await _context.Books.FindAsync(id);
        }

        public async Task Update(Book book)
        {
            _context.Books.Update(book);
            await _context.SaveChangesAsync();
        }
    }
}

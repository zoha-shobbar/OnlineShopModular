
namespace OnlineShopModular.Server.Modules.Library.Infrastructure
{
    public class LibraryDbContext : DbContext
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<LendingBook> LendingBooks { get; set; }

        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
        {
        }
    }

    public class BookRepository : IBookRepository
    {
        private readonly LibraryDbContext _context;

        public BookRepository(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task Create(Book book)
        {
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
        }

        public async Task<Book> GetById(int id)
        {
            return await _context.Books.FindAsync(id);
        }
    }

    public class LendingBookRepository : ILendingBookRepository
    {
        private readonly LibraryDbContext _context;

        public LendingBookRepository(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task Create(LendingBook lendingBook)
        {
            _context.LendingBooks.Add(lendingBook);
            await _context.SaveChangesAsync();
        }

        public async Task<LendingBook> GetById(int id)
        {
            return await _context.LendingBooks.FindAsync(id);
        }
    }
}

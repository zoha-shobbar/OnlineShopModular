using System.Threading.Tasks;
using OnlineShopModular.Server.Modules.Library.Domain.Entities;
using OnlineShopModular.Server.Modules.Library.Infrastructure.Persistence;

namespace OnlineShopModular.Server.Modules.Library.Infrastructure.Services;
public class BookService
{
    private readonly LibraryDbContext _context;

    public BookService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<BookResponseDto> CreateAsync(BookRequestDto dto, CancellationToken cancellationToken)
    {
        var book = new Book
        {
            Title = dto.Title,
            Author = dto.Author,
            IsBorrowed = false
        };

        _context.Books.AddAsync(book);
        await _context.SaveChangesAsync(cancellationToken);

        return new BookResponseDto
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            IsBorrowed = book.IsBorrowed
        };
    }

    public async Task<BookResponseDto?> GetById(Guid id)
    {
        var book = await GetById(id);
        if (book == null)
            return null;

        return new BookResponseDto
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            IsBorrowed = book.IsBorrowed
        };
    }

    public async Task LendBookAsync(Guid personId, Guid bookId, CancellationToken cancellationToken)
    {
        var book = await _context.Books.FindAsync(new object[] { bookId }, cancellationToken);
        if (book == null)
            throw new Exception("کتاب پیدا نشد");

        if (book.IsBorrowed)
            throw new Exception("کتاب قبلاً امانت داده شده است");

        // ثبت امانت
        var lending = new LendingBook
        {
            PersonId = personId,
            BookId = bookId,
            LendingDate = DateTime.UtcNow
        };

        book.IsBorrowed = true;
        _context.LendingBooks.AddAsync(lending);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

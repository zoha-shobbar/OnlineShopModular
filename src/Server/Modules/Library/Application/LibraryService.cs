using OnlineShopModular.Server.Modules.Library.Domain;
using OnlineShopModular.Shared.Dtos.Library;

namespace OnlineShopModular.Server.Modules.Library.Application
{
    public class LibraryService
    {
        private readonly IBookRepository _bookRepository;
        private readonly ILendingBookRepository _lendingBookRepository;

        public LibraryService(IBookRepository bookRepository, ILendingBookRepository lendingBookRepository)
        {
            _bookRepository = bookRepository;
            _lendingBookRepository = lendingBookRepository;
        }

        public async Task<BookResponseDto> CreateBook(BookRequestDto request)
        {
            var book = new Book
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Author = request.Author,
                IsAvailable = true
            };

            await _bookRepository.Create(book);

            return new BookResponseDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                IsAvailable = book.IsAvailable
            };
        }

        public async Task<BookResponseDto> GetBookById(Guid id)
        {
            var book = await _bookRepository.GetById(id);
            if (book == null)
            {
                return null;
            }

            return new BookResponseDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                IsAvailable = book.IsAvailable
            };
        }

        public async Task<LendBookResponseDto> LendBook(LendBookRequestDto request)
        {
            var book = await _bookRepository.GetById(request.BookId);
            if (book == null || !book.IsAvailable)
            {
                return null;
            }

            book.IsAvailable = false;
            await _bookRepository.Update(book);

            var lendingBook = new LendingBook
            {
                Id = Guid.NewGuid(),
                BookId = request.BookId,
                PersonId = request.PersonId,
                LendOn = DateTime.UtcNow
            };

            await _lendingBookRepository.Create(lendingBook);

            return new LendBookResponseDto
            {
                Id = lendingBook.Id,
                BookId = lendingBook.BookId,
                PersonId = lendingBook.PersonId,
                LendOn = lendingBook.LendOn
            };
        }
    }
}

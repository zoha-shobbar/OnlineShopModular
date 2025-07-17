
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

        public async Task<ResponseDto> GetBookById(int id)
        {
            var book = await _bookRepository.GetById(id);
            // Map to DTO
            return new ResponseDto();
        }

        public async Task<ResponseDto> CreateBook(RequestDto request)
        {
            // Map from DTO
            var book = new Book { Title = "New Book", Author = "New Author" };
            await _bookRepository.Create(book);
            return new ResponseDto();
        }

        public async Task<ResponseDto> LendBook(RequestDto request)
        {
            // Map from DTO
            var lendingBook = new LendingBook { BookId = 1, PersonId = 1 };
            await _lendingBookRepository.Create(lendingBook);
            return new ResponseDto();
        }
    }
}

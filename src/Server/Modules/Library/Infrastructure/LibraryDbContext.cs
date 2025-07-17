using Microsoft.EntityFrameworkCore;
using OnlineShopModular.Server.Modules.Library.Domain;

namespace OnlineShopModular.Server.Modules.Library.Infrastructure
{
    public class LibraryDbContext : DbContext
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<LendingBook> LendingBooks { get; set; }
    }
}

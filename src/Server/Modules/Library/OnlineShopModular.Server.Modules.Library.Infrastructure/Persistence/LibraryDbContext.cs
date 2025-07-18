using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using OnlineShopModular.Server.Modules.Library.Domain.Entities;

namespace OnlineShopModular.Server.Modules.Library.Infrastructure.Persistence;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<LendingBook> LendingBooks => Set<LendingBook>();
}

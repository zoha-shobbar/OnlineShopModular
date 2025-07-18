using OnlineShopModular.Shared.Entities;

namespace OnlineShopModular.Server.Modules.Library.Domain.Entities;

public class Book : BaseEntity
{
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public bool IsBorrowed { get; set; }
}

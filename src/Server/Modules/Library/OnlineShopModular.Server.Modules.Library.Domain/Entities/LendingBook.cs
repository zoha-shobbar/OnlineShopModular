using OnlineShopModular.Shared.Entities;
using OnlineShopModular.Server.Modules.People.Domain.Entities;

namespace OnlineShopModular.Server.Modules.Library.Domain.Entities;
public class LendingBook : BaseEntity
{
    public DateTime LendingDate { get; set; }

    public Guid PersonId { get; set; }
    public Person Person { get; set; }
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
   
}

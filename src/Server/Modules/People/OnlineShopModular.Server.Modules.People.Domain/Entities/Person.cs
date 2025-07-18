
using OnlineShopModular.Shared.Entities;

namespace OnlineShopModular.Server.Modules.People.Domain.Entities;
public class Person : BaseEntity
{
    public string FullName { get; set; } = null!;
    
}

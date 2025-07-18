using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShopModular.Shared.Dtos;
public class PersonResponseDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
}

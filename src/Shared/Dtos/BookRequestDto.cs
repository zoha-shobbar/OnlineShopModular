using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShopModular.Shared.Dtos;
public class BookRequestDto
{
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
}

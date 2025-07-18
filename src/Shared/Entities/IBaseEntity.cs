﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineShopModular.Shared.Entities;

public interface IBaseEntity 
{
    public Guid Id { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset? ModificationDate { get; set; }
    public bool IsArchived { get; set; }
}

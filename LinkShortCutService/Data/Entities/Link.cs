﻿using LinkShortCutService.Data.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace LinkShortCutService.Data.Entities;

[Index(nameof(Hash), IsUnique = true)]
[Index(nameof(Url), IsUnique = true)]
public class Link : Entity
{
    public string Url { get; set; } = null!;

    public string Hash { get; set; } = null!;

    public string? Name { get; set; }

    public string? Description { get; set; }
}

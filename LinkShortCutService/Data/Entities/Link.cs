using LinkShortCutService.Data.Entities.Base;
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

    public DateTimeOffset LastAccess { get; set; } = DateTimeOffset.Now;

    public int AccessCount { get; set; }

    public override string ToString() => $"{Name}[{Hash}] - {Url} {Description}";

    public void Deconstruct(out string Url, out string Hash, out string? Name, out string? Description)
    {
        Url         = this.Url;
        Hash        = this.Hash;
        Name        = this.Name;
        Description = this.Description;
    }

    public void Deconstruct(out string Url, out string Hash, out string? Name, out string? Description, out DateTimeOffset LastAccess, out int AccessCount)
    {
        Url         = this.Url;
        Hash        = this.Hash;
        Name        = this.Name;
        Description = this.Description;
        LastAccess  = this.LastAccess;
        AccessCount = this.AccessCount;
    }
}

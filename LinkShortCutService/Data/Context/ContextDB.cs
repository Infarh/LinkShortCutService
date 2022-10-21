using LinkShortCutService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkShortCutService.Data.Context;

public class ContextDB : DbContext
{
    public DbSet<Link> Links { get; set; } = null!;

    public ContextDB(DbContextOptions<ContextDB> opt) : base(opt) { }
}
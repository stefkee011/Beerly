using Microsoft.EntityFrameworkCore;
using Beerly.Models;

namespace Beerly.Data
{
    public class BeerlyContext : DbContext
    {
        public BeerlyContext(DbContextOptions<BeerlyContext> options) : base(options) { }

        public DbSet<Beer> Beers { get; set; }
    }
}

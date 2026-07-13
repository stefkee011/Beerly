using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Beerly.Models;
using Beerly.Data;

namespace Beerly.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BeersController : ControllerBase
    {
        private readonly BeerlyContext _context;

        public BeersController(BeerlyContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Beer>>> GetBeers()
        {
            return await _context.Beers.ToListAsync();
        }

        
        [HttpGet("{id}")]
        public async Task<ActionResult<Beer>> GetBeer(int id)
        {
            var beer = await _context.Beers.FindAsync(id);

            if (beer == null)
            {
                return NotFound();
            }

            return beer;
        }

        
        [HttpPost]
        public async Task<ActionResult<Beer>> PostBeer(Beer beer)
        {
            _context.Beers.Add(beer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBeer), new { id = beer.Id }, beer);
        }

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBeer(int id)
        {
            var beer = await _context.Beers.FindAsync(id);

            if (beer == null)
            {
                return NotFound();
            }

            _context.Beers.Remove(beer);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutBeer(int id, Beer beer)
        {

            var existingBeer = await _context.Beers.FindAsync(id);
            if (existingBeer == null)
            {
                return NotFound();
            }

            existingBeer.Name = beer.Name;
            existingBeer.Brewery = beer.Brewery;
            existingBeer.Style = beer.Style;
            existingBeer.AbvPercentage = beer.AbvPercentage;
            existingBeer.Country = beer.Country;

            await _context.SaveChangesAsync();

            return NoContent();

        }
    }
}

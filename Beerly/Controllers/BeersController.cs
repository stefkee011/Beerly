using Beerly.Data;
using Beerly.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            return await _context.Beers.Where(b => !b.IsDeleted).ToListAsync();
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


        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Beer>> PostBeer(Beer beer)
        {
            _context.Beers.Add(beer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBeer), new { id = beer.Id }, beer);
        }


        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBeer(int id)
        {
            var beer = await _context.Beers.FindAsync(id);

            if (beer == null || beer.IsDeleted)
            {
                return NotFound();
            }

            beer.IsDeleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBeer(int id, Beer beer)
        {

            var existingBeer = await _context.Beers.FindAsync(id);
            if (existingBeer == null || existingBeer.IsDeleted)
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

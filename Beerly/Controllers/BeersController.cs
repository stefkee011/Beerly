using Beerly.Data;
using Beerly.Models;
using Beerly.Services;
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
        private readonly IImageStorage _storage;

        public BeersController(BeerlyContext context, IImageStorage storage)
        {
            _context = context;
            _storage = storage;
        }


        [HttpGet]
        public async Task<ActionResult> GetBeers(string? search, int page = 1, int pageSize = 24)
        {
            
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 60);

            var query = _context.Beers
                .AsNoTracking()
                .Where(b => !b.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                // Accent-insensitive
                query = query.Where(b =>
                    EF.Functions.Collate(b.Name, "Latin1_General_100_CI_AI").Contains(term) ||
                    EF.Functions.Collate(b.Brewery, "Latin1_General_100_CI_AI").Contains(term));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(b => b.Name)
                .ThenBy(b => b.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { items, total, page, pageSize });
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Beer>> GetBeer(int id)
        {
            var beer = await _context.Beers.FindAsync(id);

            if (beer == null || beer.IsDeleted)
            {
                return NotFound();
            }

            return beer;
        }


        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Beer>> PostBeer(CreateBeerRequest request)
        {
            // Only images that went through are accepted
            if (!_storage.Exists(request.ImageUrl))
            {
                return BadRequest("Image not found. Please upload it again.");
            }

            var name = request.Name.Trim();
            var brewery = request.Brewery.Trim();

            var alreadyExists = await _context.Beers
                .AnyAsync(b => !b.IsDeleted && b.Name == name && b.Brewery == brewery);

            if (alreadyExists)
            {
                return Conflict("This beer is already in the database.");
            }

            var beer = new Beer
            {
                Name = name,
                Brewery = brewery,
                Style = request.Style.Trim(),
                AbvPercentage = request.AbvPercentage,
                Country = request.Country,
                ImageUrl = request.ImageUrl.Trim()
            };

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
            existingBeer.ImageUrl = beer.ImageUrl;

            await _context.SaveChangesAsync();

            return NoContent();

        }
    }
}

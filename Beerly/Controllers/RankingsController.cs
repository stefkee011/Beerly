using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Beerly.Data;

namespace Beerly.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RankingsController : ControllerBase
    {
        private readonly BeerlyContext _context;

        public RankingsController(BeerlyContext context)
        {
            _context = context;
        }

        [HttpGet("global")]
        public async Task<ActionResult> GetGlobalRankings()
        {
            var rankings = await _context.Reviews
                .GroupBy(r => r.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    UniqueBeerCount = g.Select(r => r.BeerId).Distinct().Count()
                })
                .OrderByDescending(r => r.UniqueBeerCount)
                .Join(_context.Users.Where(u => !u.IsDeleted),
                    ranking => ranking.UserId,
                    user => user.Id,
                    (ranking, user) => new
                    {
                        user.Username,
                        ranking.UniqueBeerCount
                    })
                .ToListAsync();

            return Ok(rankings);
        }

        [HttpGet("local/{country}")]
        public async Task<ActionResult> GetLocalRankings(string country)
        {
            var rankings = await _context.Reviews
                .Where(r => _context.Users.Any(u => u.Id == r.UserId && u.Country == country))
                .GroupBy(r => r.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    UniqueBeerCount = g.Select(r => r.BeerId).Distinct().Count()
                })
                .OrderByDescending(r => r.UniqueBeerCount)
                .Join(_context.Users.Where(u => !u.IsDeleted),
                    ranking => ranking.UserId,
                    user => user.Id,
                    (ranking, user) => new
                    {
                        user.Username,
                        ranking.UniqueBeerCount
                    })
                .ToListAsync();

            return Ok(rankings);
        }
    }
}

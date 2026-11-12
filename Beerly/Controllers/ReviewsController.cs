using Beerly.Data;
using Beerly.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Beerly.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly BeerlyContext _context;

        public ReviewsController(BeerlyContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviews()
        {
            return await _context.Reviews.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Review>> GetReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);

            if (review == null)
            {
                return NotFound();
            }

            return review;
        }

        [HttpGet("by-beer/{beerId}")]
        public async Task<ActionResult> GetReviewsForBeer(int beerId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.BeerId == beerId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    Username = r.User!.IsDeleted ? "[deleted]" : r.User.Username
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [Authorize]
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Review>> PostReview(Review review)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var userId = int.Parse(userIdClaim);

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return Unauthorized("Your account no longer exists.");
            }

            review.UserId = userId;

            var beerExists = await _context.Beers.AnyAsync(b => b.Id == review.BeerId);
            if (!beerExists)
            {
                return BadRequest("No beer exists with that BeerId.");
            }

            var alreadyReviewed = await _context.Reviews.AnyAsync(r => r.UserId == userId && r.BeerId == review.BeerId);
            if (alreadyReviewed)
            {
                return Conflict("You have already reviewed this beer. Edit your existing review instead.");
            }

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReview(int id, Review review)
        {
            var existingReview = await _context.Reviews.FindAsync(id);

            if (existingReview == null)
            {
                return NotFound();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || existingReview.UserId != int.Parse(userIdClaim))
            {
                return Forbid();
            }

            existingReview.Rating = review.Rating;
            existingReview.Comment = review.Comment;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);

            if (review == null)
            {
                return NotFound();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || review.UserId != int.Parse(userIdClaim))
            {
                return Forbid();
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
    
}
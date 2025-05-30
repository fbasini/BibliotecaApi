using AutoMapper;
using BibliotecaAPI.Data;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/ratings")]
    [Authorize]  
    public class RatingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IOutputCacheStore _cache;

        public RatingsController(ApplicationDbContext context, IMapper mapper, IOutputCacheStore cache)
        {
            _context = context;
            _mapper = mapper;
            _cache = cache;
        }

        [HttpGet("{bookId:int}", Name = "GetBookRating")]
        [AllowAnonymous]
        [EndpointSummary("Retrieves rating information for a book")]
        [SwaggerResponse(200, "Rating data retrieved successfully", typeof(BookRatingResponseDTO))]
        [SwaggerResponse(404, "Book not found")]
        [OutputCache(Tags = ["ratings"])]
        public async Task<ActionResult<BookRatingResponseDTO>> Get(int bookId)
        {
            var book = await _context.Books
                .Include(x => x.Ratings)
                .FirstOrDefaultAsync(x => x.Id == bookId);

            if (book == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? userRating = null;

            if (userId != null)
            {
                userRating = await _context.Ratings
                    .Where(x => x.BookId == bookId && x.UserId == userId)
                    .Select(x => x.Score)
                    .FirstOrDefaultAsync();
            }

            var responseDTO = new BookRatingResponseDTO
            {
                AverageRating = book.AverageRating,
                TotalRatings = book.TotalRatings,
                UserRating = userRating
            };

            return responseDTO;
        }

        [HttpPost(Name = "CreateRating")]
        [EndpointSummary("Creates a new rating for a book")]
        [SwaggerResponse(204, "Rating created successfully")]
        [SwaggerResponse(400, "Invalid request or user already rated this book")]
        [SwaggerResponse(401, "Unauthorized access")]
        public async Task<ActionResult> Post([FromQuery] int bookId, CreateRatingDTO createRatingDTO)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(x => x.BookId == bookId && x.UserId == userId);

            if (existingRating != null)
            {
                return BadRequest("You have already rated this book.");
            }

            var rating = new Rating
            {
                BookId = bookId,
                UserId = userId!,
                Score = createRatingDTO.Score
            };

            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();

            await UpdateBookRatingStats(bookId);

            await _cache.EvictByTagAsync("books-get", default);

            return NoContent();
        }

        private async Task UpdateBookRatingStats(int bookId)
        {
            var book = await _context.Books
                .Include(x => x.Ratings)
                .FirstOrDefaultAsync(x => x.Id == bookId);

            if (book != null)
            {
                book.AverageRating = book.Ratings.Average(x => x.Score);
                book.TotalRatings = book.Ratings.Count;
                await _context.SaveChangesAsync();
            }
        }

        [HttpPut(Name = "UpdateRating")]
        [EndpointSummary("Updates an existing rating")]
        [SwaggerResponse(204, "Rating updated successfully")]
        [SwaggerResponse(404, "Rating not found")]
        [SwaggerResponse(401, "Unauthorized access")]
        public async Task<ActionResult> Put([FromQuery] int bookId, CreateRatingDTO updateRatingDTO)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(x => x.BookId == bookId && x.UserId == userId);

            if (existingRating == null)
            {
                return NotFound("Rating not found.");
            }

            existingRating.Score = updateRatingDTO.Score;
            await _context.SaveChangesAsync();

            await UpdateBookRatingStats(bookId);
            await _cache.EvictByTagAsync("books-get", default);

            return NoContent();
        }
    }
}

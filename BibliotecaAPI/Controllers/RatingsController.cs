using AutoMapper;
using BibliotecaAPI.Data;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using BibliotecaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly IUserService _userService;

        public RatingsController(ApplicationDbContext context, IMapper mapper, IOutputCacheStore cache, IUserService userService)
        {
            _context = context;
            _mapper = mapper;
            _cache = cache;
            _userService = userService;
        }

        [HttpGet("{bookId:int}", Name = "GetBookRating")]
        //[AllowAnonymous]
        [EndpointSummary("Retrieves rating information for a book")]
        [SwaggerResponse(200, "Rating data retrieved successfully", typeof(BookRatingResponseDTO))]
        [SwaggerResponse(404, "Book not found")]
        [OutputCache(Tags = ["ratings"])]
        public async Task<ActionResult<BookRatingResponseDTO>> Get(int bookId)
        {
            var book = await _context.Books
        .Include(x => x.Ratings)
        .FirstOrDefaultAsync(x => x.Id == bookId);

            if (book == null) return NotFound();

            int? userRating = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userService.GetUser();
                if (user != null)
                {
                    userRating = await _context.Ratings
                        .Where(x => x.BookId == bookId && x.UserId == user.Id)
                        .Select(x => (int?)x.Score)
                        .FirstOrDefaultAsync();
                }
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
        [Authorize]
        [EndpointSummary("Creates a new rating for a book")]
        [SwaggerResponse(204, "Rating created successfully")]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(401, "Unauthorized access")]
        [SwaggerResponse(404, "Book not found")]
        public async Task<ActionResult> Post([FromQuery] int bookId, [FromBody] CreateRatingDTO createRatingDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.GetUser();
            if (user is null)
                return Unauthorized();

            if (!await _context.Books.AnyAsync(x => x.Id == bookId))
                return NotFound("Book not found");

            var rating = new Rating
            {
                BookId = bookId,
                UserId = user.Id, 
                Score = createRatingDTO.Score,
                CreatedAt = DateTime.UtcNow
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
                book.TotalRatings = book.Ratings.Count;

                book.AverageRating = book.TotalRatings > 0
                    ? book.Ratings.Average(x => x.Score)
                    : 0; 

                await _context.SaveChangesAsync();
            }
        }

        [HttpPut(Name = "UpdateRating")]
        [Authorize]
        [EndpointSummary("Updates an existing rating")]
        [SwaggerResponse(204, "Rating updated successfully")]
        [SwaggerResponse(404, "Rating not found")]
        [SwaggerResponse(401, "Unauthorized access")]
        public async Task<ActionResult> Put([FromQuery] int bookId, [FromBody] CreateRatingDTO updateRatingDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.GetUser();
            if (user is null)
                return Unauthorized();

            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.BookId == bookId && r.UserId == user.Id);

            if (existingRating is null)
                return NotFound("Rating not found. First create a rating using POST.");

            existingRating.Score = updateRatingDTO.Score;
            await _context.SaveChangesAsync();

            await UpdateBookRatingStats(bookId);
            await _cache.EvictByTagAsync("ratings", default);
            await _cache.EvictByTagAsync("books-get", default);

            return NoContent();
        }

        [HttpDelete("{bookId:int}", Name = "DeleteRating")]
        [Authorize]
        [EndpointSummary("Deletes a rating for a book")]
        [SwaggerResponse(204, "Rating deleted successfully")]
        [SwaggerResponse(404, "Rating not found")]
        [SwaggerResponse(401, "Unauthorized access")]
        public async Task<ActionResult> Delete(int bookId)
        {
            var user = await _userService.GetUser();
            if (user is null)
                return Unauthorized();

            var rating = await _context.Ratings
                .FirstOrDefaultAsync(x => x.BookId == bookId && x.UserId == user.Id);

            if (rating is null)
                return NotFound("Rating not found.");

            _context.Ratings.Remove(rating);
            await _context.SaveChangesAsync();

            await UpdateBookRatingStats(bookId);

            await _cache.EvictByTagAsync($"ratings-{bookId}", default);
            await _cache.EvictByTagAsync($"user-ratings-{user.Id}-{bookId}", default);
            await _cache.EvictByTagAsync("ratings", default);
            await _cache.EvictByTagAsync("books-get", default);

            return NoContent();
        }
    }
}

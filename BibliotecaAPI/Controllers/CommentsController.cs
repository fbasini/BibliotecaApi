using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaAPI.Data;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using BibliotecaAPI.Services;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.JsonPatch;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/books/{bookId:int}/comments")]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IUserService userService;
        private readonly IOutputCacheStore outputCacheStore;
        private const string cache = "comments-get";

        public CommentsController(ApplicationDbContext context, IMapper mapper,
            IUserService userService, IOutputCacheStore outputCacheStore)
        {
            this.context = context;
            this.mapper = mapper;
            this.userService = userService;
            this.outputCacheStore = outputCacheStore;
        }

        [HttpGet(Name = "GetComments")]
        [AllowAnonymous]
        [EndpointSummary("Retrieves comments for a book")]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<List<CommentDTO>>> Get(int bookId)
        {
            var bookExists = await context.Books.AnyAsync(x => x.Id == bookId);

            if (!bookExists)
            {
                return NotFound();
            }

            var comments = await context.Comments
                .Include(x => x.User)
                .Where(x => x.BookId == bookId)
                .OrderByDescending(x => x.PostedAt)
                .ToListAsync();

            return mapper.Map<List<CommentDTO>>(comments);
        }

        [HttpGet("{id:int}", Name = "GetComment")]
        [AllowAnonymous]
        [EndpointSummary("Retrieves a specific comment")]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<CommentDTO>> GetById(Guid id)
        {
            var comment = await context.Comments
                                    .Include(x => x.User)
                                    .FirstOrDefaultAsync(x => x.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            return mapper.Map<CommentDTO>(comment);
        }

        [HttpPost(Name = "CreateComment")]
        [EndpointSummary("Creates a new comment for a book")]
        public async Task<ActionResult> Post(int bookId, CreateCommentDTO createCommentDTO)
        {
            var bookExists = await context.Books.AnyAsync(x => x.Id == bookId);

            if (!bookExists)
            {
                return NotFound();
            }

            var user = await userService.GetUser();

            if (user is null)
            {
                return NotFound();
            }

            var comment = mapper.Map<Comment>(createCommentDTO);
            comment.BookId = bookId;
            comment.PostedAt = DateTime.UtcNow;
            comment.UserId = user.Id;
            context.Add(comment);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            var commentDTO = mapper.Map<CommentDTO>(comment);

            return CreatedAtRoute("GetComment", new { id = comment.Id, bookId }, commentDTO);
        }

        [HttpPatch("{id}", Name = "PatchComment")]
        [EndpointSummary("Partially updates a comment")]
        public async Task<ActionResult> Patch(Guid id, int bookId, JsonPatchDocument<CommentPatchDTO> patchDoc)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }

            var bookExists = await context.Books.AnyAsync(x => x.Id == bookId);

            if (!bookExists)
            {
                return NotFound();
            }

            var user = await userService.GetUser();

            if (user is null)
            {
                return NotFound();
            }


            var commentDB = await context.Comments.FirstOrDefaultAsync(x => x.Id == id);

            if (commentDB is null)
            {
                return NotFound();
            }

            if (commentDB.UserId != user.Id)
            {
                return Forbid();
            }

            var commentPatchDTO = mapper.Map<CommentPatchDTO>(commentDB);

            patchDoc.ApplyTo(commentPatchDTO, ModelState);

            var isValid = TryValidateModel(commentPatchDTO);

            if (!isValid)
            {
                return ValidationProblem();
            }

            mapper.Map(commentPatchDTO, commentDB);

            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }

        [HttpPut("{id:int}", Name = "UpdateComment")]
        [EndpointSummary("Updates a comment")]
        public async Task<ActionResult> Put(int bookId, Guid id, CreateCommentDTO createCommentDTO)
        {
            var bookExists = await context.Books.AnyAsync(x => x.Id == bookId);

            if (!bookExists)
            {
                return NotFound();
            }

            var commentDB = await context.Comments.FirstOrDefaultAsync(x => x.Id == id);

            if (commentDB is null)
            {
                return NotFound();
            }

            var comment = mapper.Map<Comment>(createCommentDTO);
            comment.Id = id;
            comment.BookId = bookId;

            context.Update(comment);
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}", Name = "DeleteComment")]
        [EndpointSummary("Deletes a comment")]
        public async Task<ActionResult> Delete(Guid id, int bookId)
        {
            var bookExists = await context.Books.AnyAsync(x => x.Id == bookId);

            if (!bookExists)
            {
                return NotFound();
            }

            var user = await userService.GetUser();

            if (user is null)
            {
                return NotFound();
            }

            var commentDB = await context.Comments.FirstOrDefaultAsync(x => x.Id == id);

            if (commentDB is null)
            {
                return NotFound();
            }

            if (commentDB.UserId != user.Id)
            {
                return Forbid();
            }

            commentDB.IsDeleted = true;
            context.Update(commentDB);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }
    }
}

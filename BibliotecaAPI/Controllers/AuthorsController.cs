using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaAPI.Data;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using BibliotecaAPI.Services;
using BibliotecaAPI.Utilities;
using Microsoft.AspNetCore.OutputCaching;
using System.ComponentModel;
using Microsoft.AspNetCore.JsonPatch;
using System.Linq.Dynamic.Core;
using Swashbuckle.AspNetCore.Annotations;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/authors")]
    [Authorize(Policy = "isadmin")]
    public class AuthorsController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IFileStorage fileStorage;
        private readonly ILogger<AuthorsController> logger;
        private readonly IOutputCacheStore outputCacheStore;
        private readonly IAuthorService authorService;
        private const string container = "authors";
        private const string cache = "authors-get";

        public AuthorsController(ApplicationDbContext context, IMapper mapper,
            IFileStorage fileStorage, ILogger<AuthorsController> logger,
            IOutputCacheStore outputCacheStore, IAuthorService authorService)
        {
            this.context = context;
            this.mapper = mapper;
            this.fileStorage = fileStorage;
            this.logger = logger;
            this.outputCacheStore = outputCacheStore;
            this.authorService = authorService;
        }


        [HttpGet(Name = "GetAuthors")] // api/authors
        [AllowAnonymous]
        [EndpointSummary("Retrieves a list of all authors")]
        [SwaggerResponse(200, "List of authors retrieved successfully.", typeof(IEnumerable<AuthorDTO>))]
        [OutputCache(Tags = [cache])]
        [ServiceFilter<HATEOASAuthorsAttribute>]
        public async Task<IEnumerable<AuthorDTO>> Get([FromQuery] PaginationDTO paginationDTO)
        {
            return await authorService.Get(paginationDTO);
        }

        [HttpGet("{id:int}", Name = "GetAuthor")] // api/authors/id
        [AllowAnonymous]
        [EndpointSummary("Retrieves an author by ID")]
        [SwaggerResponse(200, "Author details retrieved successfully.", typeof(AuthorWithBooksDTO))]
        [SwaggerResponse(404, "Author not found.")]
        [ProducesResponseType<AuthorWithBooksDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [OutputCache(Tags = [cache])]
        [ServiceFilter<HATEOASAuthorAttribute>()]
        public async Task<ActionResult<AuthorWithBooksDTO>> Get(int id)
        {
            var author = await context.Authors
                .Include(x => x.Books)
                .ThenInclude(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (author == null)
            {
                return NotFound();
            }

            var authorDTO = mapper.Map<AuthorWithBooksDTO>(author);

            return authorDTO;
        }

        [HttpGet("filter", Name = "FilterAuthors")]
        [AllowAnonymous]
        [EndpointSummary("Filter authors")]
        [SwaggerResponse(200, "Filtered list of authors retrieved successfully.", typeof(IEnumerable<AuthorDTO>))]
        [SwaggerResponse(400, "Invalid request.")]
        public async Task<ActionResult> Filter([FromQuery] AuthorFilterDTO authorFilterDTO)
        {
            var queryable = context.Authors.AsQueryable();

            if (!string.IsNullOrEmpty(authorFilterDTO.FirstNames))
            {
                queryable = queryable.Where(x => x.FirstName.Contains(authorFilterDTO.FirstNames));
            }

            if (!string.IsNullOrEmpty(authorFilterDTO.LastNames))
            {
                queryable = queryable.Where(x => x.LastName.Contains(authorFilterDTO.LastNames));
            }

            if (!string.IsNullOrEmpty(authorFilterDTO.Identification))
            {
                queryable = queryable.Where(x => x.Identification != null &&
                                                x.Identification.Contains(authorFilterDTO.Identification));
            }

            if (authorFilterDTO.IncludeBooks)
            {
                queryable = queryable.Include(x => x.Books).ThenInclude(x => x.Book);
            }

            if (authorFilterDTO.HasPhoto.HasValue)
            {
                if (authorFilterDTO.HasPhoto.Value)
                {
                    queryable = queryable.Where(x => x.Photo != null);
                }
                else
                {
                    queryable = queryable.Where(x => x.Photo == null);
                }
            }

            if (authorFilterDTO.HasBooks.HasValue)
            {
                if (authorFilterDTO.HasBooks.Value)
                {
                    queryable = queryable.Where(x => x.Books.Any());
                }
                else
                {
                    queryable = queryable.Where(x => !x.Books.Any());
                }
            }

            if (!string.IsNullOrEmpty(authorFilterDTO.BookTitle))
            {
                queryable = queryable.Where(x =>
                    x.Books.Any(y => y.Book!.Title.Contains(authorFilterDTO.BookTitle)));
            }

            if (!string.IsNullOrEmpty(authorFilterDTO.SortField))
            {
                var sortOrder = authorFilterDTO.IsAscending ? "ascending" : "descending";

                try
                {
                    queryable = queryable.OrderBy($"{authorFilterDTO.SortField} {sortOrder}");
                }
                catch (Exception ex)
                {
                    queryable = queryable.OrderBy(x => x.FirstName);
                    logger.LogError(ex.Message, ex);
                }
            }
            else
            {
                queryable = queryable.OrderBy(x => x.FirstName);
            }

            var authors = await queryable
                    .Paginate(authorFilterDTO.PaginationDTO).ToListAsync();

            if (authorFilterDTO.IncludeBooks)
            {
                var authorsDTO = mapper.Map<IEnumerable<AuthorWithBooksDTO>>(authors);
                return Ok(authorsDTO);
            }
            else
            {
                var authorsDTO = mapper.Map<IEnumerable<AuthorDTO>>(authors);
                return Ok(authorsDTO);
            }

         

        }

        [HttpPost(Name = "CreateAuthor")]
        [EndpointSummary("Creates an author")]
        [SwaggerResponse(201, "Author created successfully.", typeof(AuthorDTO))]
        [SwaggerResponse(400, "Invalid request.")]
        public async Task<ActionResult> Post([FromBody] CreateAuthorDTO createAuthorDTO)
        {
            var author = mapper.Map<Author>(createAuthorDTO);
            context.Add(author);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            var authorDTO = mapper.Map<AuthorDTO>(author);
            return CreatedAtRoute("GetAuthor", new { id = author.Id }, authorDTO);
        }

        [HttpPost("with-photo", Name = "CreateAuthorWithPhoto")]
        [EndpointSummary("Creates an author with a photo")]
        [SwaggerResponse(201, "Author with photo created successfully.", typeof(AuthorDTO))]
        [SwaggerResponse(400, "Invalid request.")]
        public async Task<ActionResult> PostWithPhoto([FromForm]
            CreateAuthorWithPhotoDTO createAuthorDTO)
        {
            var author = mapper.Map<Author>(createAuthorDTO);

            if (createAuthorDTO.Photo is not null)
            {
                var url = await fileStorage.StoreFile(container,
                    createAuthorDTO.Photo);
                author.Photo = url;
            }

            context.Add(author);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);

            var authorDTO = mapper.Map<AuthorDTO>(author);
            return CreatedAtRoute("GetAuthor", new { id = author.Id }, authorDTO);
        }

        [HttpPut("{id:int}", Name = "UpdateAuthor")] // api/authors/id
        [EndpointSummary("Updates an author")]
        [SwaggerResponse(204, "Author updated successfully.")]
        [SwaggerResponse(400, "Invalid request.")]
        [SwaggerResponse(404, "Author not found.")]
        public async Task<ActionResult> Put([FromForm] CreateAuthorWithPhotoDTO createAuthorDTO, int id)
        {
            var exists = await context.Authors.AnyAsync(x => x.Id == id);

            if (!exists)
            {
                return NotFound();
            }

            var author = mapper.Map<Author>(createAuthorDTO);
            author.Id = id;

            if (createAuthorDTO.Photo is not null)
            {
                var currentPhoto = await context.Authors
                    .Where(x => x.Id == id)
                    .Select(x => x.Photo).FirstAsync();

                var url = await fileStorage.Edit(currentPhoto, container,
                    createAuthorDTO.Photo);
                author.Photo = url;
            }

            context.Update(author);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }

        [HttpPatch("{id:int}", Name = "PatchAutor")]
        [EndpointSummary("Partially updates an author")]
        [SwaggerResponse(204, "Author partially updated successfully.")]
        [SwaggerResponse(400, "Invalid request.")]
        [SwaggerResponse(404, "Author not found.")]
        public async Task<ActionResult> Patch(int id, JsonPatchDocument<AuthorPatchDTO> patchDoc)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }

            var authorDB = await context.Authors.FirstOrDefaultAsync(x => x.Id == id);

            if (authorDB is null)
            {
                return NotFound();
            }

            var authorPatchDTO = mapper.Map<AuthorPatchDTO>(authorDB);

            patchDoc.ApplyTo(authorPatchDTO, ModelState);

            var isValid = TryValidateModel(authorPatchDTO);

            if (!isValid)
            {
                return ValidationProblem();
            }

            mapper.Map(authorPatchDTO, authorDB);

            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }

        [HttpDelete("{id:int}", Name = "DeleteAuthor")]
        [EndpointSummary("Deletes an author")]
        [SwaggerResponse(204, "Author deleted successfully.")]
        [SwaggerResponse(404, "Author not found.")]
        public async Task<ActionResult> Delete(int id)
        {
            var author = await context.Authors.FirstOrDefaultAsync(x => x.Id == id);

            if (author is null)
            {
                return NotFound();
            }

            context.Remove(author);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            await fileStorage.Delete(author.Photo, container);

            return NoContent();
        }
    }
}

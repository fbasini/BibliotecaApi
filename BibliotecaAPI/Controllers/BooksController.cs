using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaAPI.Data;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OutputCaching;
using BibliotecaAPI.Utilities;
using Swashbuckle.AspNetCore.Annotations;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/books")]
    [Authorize(Policy = "isadmin")]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IOutputCacheStore outputCacheStore;
        private const string cache = "books-get";

        public BooksController(ApplicationDbContext context, IMapper mapper,
            IOutputCacheStore outputCacheStore)
        {
            this.context = context;
            this.mapper = mapper;
            this.outputCacheStore = outputCacheStore;
        }

        [HttpGet(Name = "GetBooks")]
        [AllowAnonymous]
        [EndpointSummary("Retrieves a list of all books")]
        [SwaggerResponse(200, "List of books retrieved successfully.", typeof(IEnumerable<BookDTO>))]
        [OutputCache(Tags = [cache])]
        public async Task<IEnumerable<BookDTO>> Get([FromQuery] PaginationDTO paginationDTO)
        {
            var queryable = context.Books.AsQueryable();
            
            await HttpContext.InsertPaginationParamsInHeader(queryable);
            
            var books = await queryable
                        .OrderBy(x => x.Title)
                        .Paginate(paginationDTO).ToListAsync();
            
            var booksDTO = mapper.Map<IEnumerable<BookDTO>>(books);
            
            return booksDTO;
        }

        [HttpGet("{id:int}", Name = "GetBook")]
        [AllowAnonymous]
        [EndpointSummary("Retrieves a book by Id")]
        [SwaggerResponse(200, "Book details retrieved successfully.", typeof(BookWithAuthorsDTO))]
        [SwaggerResponse(404, "Book not found.")]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<BookWithAuthorsDTO>> Get(int id)
        {
            var book = await context.Books
                .Include(x => x.Authors)
                .ThenInclude(x => x.Author)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            var bookDTO = mapper.Map<BookWithAuthorsDTO>(book);

            return bookDTO;
        }

        [HttpPost(Name = "CreateBook")]
        [EndpointSummary("Creates a book")]
        [SwaggerResponse(201, "Book created successfully.", typeof(BookDTO))]
        [SwaggerResponse(400, "Invalid request.")]
        [ServiceFilter<BookValidationFilter>()]
        public async Task<ActionResult> Post(CreateBookDTO createBookDTO)
        {
            var book = mapper.Map<Book>(createBookDTO);
            AssignAuthorOrder(book);

            context.Add(book);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);

            var bookDTO = mapper.Map<BookDTO>(book);

            return CreatedAtRoute("GetBook", new { id = book.Id }, bookDTO);
        }

        private void AssignAuthorOrder(Book book)
        {
            if (book.Authors is not null)
            {
                for (int i = 0; i < book.Authors.Count; i++)
                {
                    book.Authors[i].Order = i;
                }
            }
        }

        [HttpPut("{id:int}", Name = "UpdateBook")]
        [EndpointSummary("Updates a book")]
        [SwaggerResponse(204, "Book updated successfully.")]
        [SwaggerResponse(400, "Invalid request.")]
        [SwaggerResponse(404, "Book not found.")]
        [ServiceFilter<BookValidationFilter>]
        public async Task<ActionResult> Put(int id, CreateBookDTO createBookDTO)
        {
            var bookDB = await context.Books
                            .Include(x => x.Authors)
                            .FirstOrDefaultAsync(x => x.Id == id);

            if (bookDB is null)
            {
                return NotFound();
            }

            bookDB = mapper.Map(createBookDTO, bookDB);
            AssignAuthorOrder(bookDB);

            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }

        //[HttpPatch("{id:int}", Name = "patchLibro")]
        //public async Task<ActionResult> Patch(int id, JsonPatchDocument<LibroPatchDTO> patchDocument)
        //{
        //    if (patchDocument == null)
        //    {
        //        return BadRequest();
        //    }

        //    var libroDB = await context.Libros.FirstOrDefaultAsync(x => x.Id == id);

        //    if (libroDB == null)
        //    {
        //        return NotFound();
        //    }

        //    var libroDTO = mapper.Map<LibroPatchDTO>(libroDB);

        //    patchDocument.ApplyTo(libroDTO, ModelState);

        //    var esValido = TryValidateModel(libroDTO);

        //    if (!esValido)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    mapper.Map(libroDTO, libroDB);

        //    await context.SaveChangesAsync();
        //    return NoContent();
        //}

        [HttpDelete("{id:int}", Name = "DeleteBook")]
        [EndpointSummary("Deletes a book")]
        [SwaggerResponse(200, "Book deleted successfully.")]
        //[SwaggerResponse(204, "Book deleted successfully.")]
        [SwaggerResponse(404, "Book not found.")]
        public async Task<ActionResult> Delete(int id)
        {
            var deletedRecords = await context.Books.Where(x => x.Id == id).ExecuteDeleteAsync();

            if (deletedRecords == 0)
            {
                return NotFound();
            }

            await outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }
    }
}

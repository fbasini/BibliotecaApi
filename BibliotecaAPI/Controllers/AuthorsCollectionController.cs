using AutoMapper;
using BibliotecaAPI.Data;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/authors-collection")]
    [Authorize(Policy = "isadmin")]
    public class AuthorsCollectionController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public AuthorsCollectionController(ApplicationDbContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet("{ids}", Name = "GetAuthorsByIds")] // api/authors-collection/1,2,3
        [EndpointSummary("Retrieves a collection of authors")]
        public async Task<ActionResult<List<AuthorWithBooksDTO>>> Get(string ids)
        {
            var idsCollection = new List<int>();

            foreach (var id in ids.Split(","))
            {
                if (int.TryParse(id, out int idInt))
                {
                    idsCollection.Add(idInt);
                }
            }

            if (!idsCollection.Any())
            {
                ModelState.AddModelError(nameof(ids), "No valid IDs were provided");
                return ValidationProblem();
            }

            var authors = await context.Authors
                            .Include(x => x.Books)
                                .ThenInclude(x => x.Book)
                            .Where(x => idsCollection.Contains(x.Id))
                            .ToListAsync();

            if (authors.Count != idsCollection.Count)
            {
                return NotFound();
            }

            var authorsDTO = mapper.Map<List<AuthorWithBooksDTO>>(authors);
            return authorsDTO;
        }

        [HttpPost(Name = "CreateAuthors")]
        [EndpointSummary("Creates a collection of authors")]
        public async Task<ActionResult> Post(IEnumerable<CreateAuthorDTO> createAuthorDTO)
        {
            var authors = mapper.Map<IEnumerable<Author>>(createAuthorDTO);
            context.AddRange(authors);
            await context.SaveChangesAsync();

            var authorsDTO = mapper.Map<IEnumerable<AuthorDTO>>(authors);
            var ids = authors.Select(x => x.Id);
            var idsString = string.Join(",", ids);
            return CreatedAtRoute("GetAuthorsByIds", new { ids = idsString }, authorsDTO);
        }
    }
}

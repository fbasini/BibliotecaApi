using AutoMapper;
using BibliotecaAPI.Data;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Utilities;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Services
{
    public class AuthorService : IAuthorService
    {
        private readonly ApplicationDbContext context;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IMapper mapper;

        public AuthorService(ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor, IMapper mapper)
        {
            this.context = context;
            this.httpContextAccessor = httpContextAccessor;
            this.mapper = mapper;
        }

        public async Task<IEnumerable<AuthorDTO>> Get(PaginationDTO paginationDTO)
        {
            var queryable = context.Authors.AsQueryable();
            await httpContextAccessor.HttpContext!.InsertPaginationParamsInHeader(queryable);
            var authors = await queryable
                        .OrderBy(x => x.FirstName)
                        .Paginate(paginationDTO).ToListAsync();
            var authorsDTO = mapper.Map<IEnumerable<AuthorDTO>>(authors);
            return authorsDTO;
        }
    }
}

using BibliotecaAPI.DTOs;

namespace BibliotecaAPI.Services
{
    public interface IAuthorService
    {
        Task<IEnumerable<AuthorDTO>> Get(PaginationDTO paginationDTO);
    }
}

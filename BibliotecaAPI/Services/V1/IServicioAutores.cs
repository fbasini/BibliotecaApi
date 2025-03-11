using BibliotecaAPI.DTOs;

namespace BibliotecaAPI.Services.V1
{
    public interface IServicioAutores
    {
        Task<IEnumerable<AutorDTO>> Get(PaginacionDTO paginacionDTO);
    }
}

using BibliotecaAPI.DTOs;

namespace BibliotecaAPI.Services
{
    public interface ILinkGenerator
    {
        Task GenerateLinks(AuthorDTO authorDTO);
        Task<ResourcesListDTO<AuthorDTO>> GenerateLinks(List<AuthorDTO> authors);
    }
}

using BibliotecaAPI.Entities;

namespace BibliotecaAPI.Services
{
    public interface IServiciosUsuarios
    {
        Task<Usuario?> ObtenerUsuario();
    }
}

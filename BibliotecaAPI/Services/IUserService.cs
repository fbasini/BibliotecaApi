using BibliotecaAPI.Entities;

namespace BibliotecaAPI.Services
{
    public interface IUserService
    {
        Task<User?> GetUser();
    }
}

namespace BibliotecaAPI.Services
{
    public interface IFileStorage
    {
        Task Delete(string? path, string container);
        Task<string> StoreFile(string container, IFormFile file);
        async Task<string> Edit(string? path, string container, IFormFile file)
        {
            await Delete(path, container);
            return await StoreFile(container, file);
        }
    }
}

namespace BibliotecaAPI.DTOs
{
    public class ResourcesListDTO<T> : ResourceDTO where T : ResourceDTO
    {
        public IEnumerable<T> Values { get; set; } = [];
    }
}

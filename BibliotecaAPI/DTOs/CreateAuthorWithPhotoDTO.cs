namespace BibliotecaAPI.DTOs
{
    public class CreateAuthorWithPhotoDTO : CreateAuthorDTO
    {
        public IFormFile? Photo { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class CreateCommentDTO
    {
        [Required]
        public required string Body { get; set; }
    }
}

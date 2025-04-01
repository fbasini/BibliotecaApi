using System.ComponentModel.DataAnnotations;
using BibliotecaAPI.Validations;

namespace BibliotecaAPI.DTOs
{
    public class CreateBookDTO
    {
        [Required]
        [StringLength(250, ErrorMessage = "The field {0} must have {1} characters or fewer")]
        public required string Title { get; set; }
        public List<int> AuthorsIds { get; set; } = [];
    }
}

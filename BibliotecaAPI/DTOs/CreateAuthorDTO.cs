using System.ComponentModel.DataAnnotations;
using BibliotecaAPI.Validations;

namespace BibliotecaAPI.DTOs
{
    public class CreateAuthorDTO
    {
        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(150, ErrorMessage = "The field {0} must have {1} characters or fewer")]
        [FirstLetterUppercase]
        public required string FirstName { get; set; }
        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(150, ErrorMessage = "The field {0} must have {1} characters or fewer")]
        [FirstLetterUppercase]
        public required string LastName { get; set; }
        [StringLength(20, ErrorMessage = "The field {0} must have {1} characters or fewer")]
        public string? Identification { get; set; }
        public List<CreateBookDTO> Books { get; set; } = [];
    }
}

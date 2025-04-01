using BibliotecaAPI.Validations;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class AuthorPatchDTO
    {
        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(150, ErrorMessage = "The field {0} must have {1} characters or fewer")]
        [FirstLetterUppercase]
        public required string FirstNames { get; set; }
        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(150, ErrorMessage = "The field {0} must have {1} characters or fewer")]
        [FirstLetterUppercase]
        public required string LastNames { get; set; }
        [StringLength(20, ErrorMessage = "The field {0} must have {1} characters or fewer")]
        public string? Identification { get; set; }
    }
}

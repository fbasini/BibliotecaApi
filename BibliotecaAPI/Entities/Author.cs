using System.ComponentModel.DataAnnotations;
using BibliotecaAPI.Validations;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Entities
{
    public class Author
    {
        public int Id { get; set; }
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
        [Unicode(false)]
        public string? Photo { get; set; }
        public List<AuthorBook> Books { get; set; } = [];
    }
}

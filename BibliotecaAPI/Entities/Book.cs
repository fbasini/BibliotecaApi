using System.ComponentModel.DataAnnotations;
using BibliotecaAPI.Validations;

namespace BibliotecaAPI.Entities
{
    public class Book
    {
        public int Id { get; set; }
        [Required]
        [StringLength(250, ErrorMessage = "The field {0} must have {1} characters or fewer")]
        public required string Title { get; set; }
        public List<AuthorBook> Authors { get; set; } = [];
        public List<Comment> Comments { get; set; } = [];
        public double AverageRating { get; set; } = 0.0;
        public int TotalRatings { get; set; } = 0;
        public List<Rating> Ratings { get; set; } = [];
    }
}

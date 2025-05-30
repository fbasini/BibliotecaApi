using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;
using BibliotecaAPI.Validations;

namespace BibliotecaAPI.DTOs
{
    public class BookDTO
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public double AverageRating { get; set; }  
        public int TotalRatings { get; set; }
    }
}

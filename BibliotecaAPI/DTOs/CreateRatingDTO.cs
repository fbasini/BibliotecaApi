using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class CreateRatingDTO
    {
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Score { get; set; }
    }
}

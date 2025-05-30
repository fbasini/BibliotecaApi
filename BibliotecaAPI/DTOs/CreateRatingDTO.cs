using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class CreateRatingDTO
    {
        [Range(0.5, 5, ErrorMessage = "Rating must be between 0.5 and 5")]
        public decimal Score { get; set; }
    }
}

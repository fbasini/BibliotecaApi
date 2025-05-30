namespace BibliotecaAPI.DTOs
{
    public class BookRatingResponseDTO
    {
        public decimal AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public int? UserRating { get; set; }
    }
}

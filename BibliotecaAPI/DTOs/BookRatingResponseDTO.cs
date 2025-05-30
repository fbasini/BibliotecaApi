namespace BibliotecaAPI.DTOs
{
    public class BookRatingResponseDTO
    {
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public int? UserRating { get; set; }
    }
}

namespace BibliotecaAPI.Entities
{
    public class Rating
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public Book Book { get; set; } = null!;
        public string UserId { get; set; } = null!;  
        public User User { get; set; } = null!;
        public decimal Score { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

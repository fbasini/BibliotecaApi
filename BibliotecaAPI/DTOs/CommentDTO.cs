namespace BibliotecaAPI.DTOs
{
    public class CommentDTO
    {
        public Guid Id { get; set; }
        public required string Body { get; set; }
        public DateTime PostedAt { get; set; }
        public required string UserId { get; set; }
        public required string UserEmail { get; set; }
    }
}

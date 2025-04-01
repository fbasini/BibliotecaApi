namespace BibliotecaAPI.Entities
{
    public class Error
    {
        public Guid Id { get; set; }
        public required string ErrorMessage { get; set; }
        public string? StrackTrace { get; set; }
        public DateTime Date { get; set; }
    }
}

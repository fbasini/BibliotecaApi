namespace BibliotecaAPI.DTOs
{
    public class UpdateUserDTO
    {
        public DateTime BirthDate { get; set; }
        public string? CurrentPassword { get; set; }  
        public string? NewPassword { get; set; }
    }
}

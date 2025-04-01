using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class EditClaimDTO
    {
        [EmailAddress]
        [Required]
        public required string Email { get; set; }
    }
}

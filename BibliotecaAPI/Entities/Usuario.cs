using Microsoft.AspNetCore.Identity;

namespace BibliotecaAPI.Entities
{
    public class Usuario : IdentityUser
    {
        public DateTime FechaNacimiento { get; set; }
    }
}

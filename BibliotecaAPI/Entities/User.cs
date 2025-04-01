using Microsoft.AspNetCore.Identity;

namespace BibliotecaAPI.Entities
{
    public class User : IdentityUser
    {
        public DateTime BirthDate { get; set; }
    }
}

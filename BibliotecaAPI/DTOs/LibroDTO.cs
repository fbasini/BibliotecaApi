using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;
using BibliotecaAPI.Validations;

namespace BibliotecaAPI.DTOs
{
    public class LibroDTO
    {
        public int Id { get; set; }
        public required string Titulo { get; set; }
    }
}


using System.ComponentModel.DataAnnotations;

namespace Huellitas.Models
{
    public class Mascota
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(80)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La especie es obligatoria")]
        [StringLength(40)]
        public string Especie { get; set; } = string.Empty;

        [Required(ErrorMessage = "La raza es obligatoria")]
        [StringLength(60)]
        public string Raza { get; set; } = string.Empty;

        [Required(ErrorMessage = "El sexo es obligatorio")]
        [StringLength(20)]
        public string Sexo { get; set; } = string.Empty;

        public DateTime? FechaNacimiento { get; set; }

        [StringLength(500)]
        public string? FotoUrl { get; set; }

        public int PropietarioId { get; set; }

       public Propietario? Propietario { get; set; }
    }
}

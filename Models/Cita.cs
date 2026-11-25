
using System.ComponentModel.DataAnnotations;

namespace Huellitas.Models
{
    public class Cita
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una mascota")]
        public int MascotaId { get; set; }

        [Required(ErrorMessage = "La fecha y hora son obligatorias")]
        public DateTime FechaHora { get; set; }

        [Required(ErrorMessage = "El motivo de la cita es obligatorio")]
        [StringLength(250)]
        public string Motivo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estado es obligatorio")]
        [StringLength(20)]
        public string Estado { get; set; } = "Pendiente";

        [Range(1, int.MaxValue,
            ErrorMessage = "El número de consultorio debe ser mayor que cero")]
        public int NumeroConsultorio { get; set; }

        [Display(Name = "Veterinario")]
        public int? PersonalId { get; set; }

        public Mascota? Mascota { get; set; }

        public Personal? Personal { get; set; }
    }
}
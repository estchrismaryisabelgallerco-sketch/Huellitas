
using System.ComponentModel.DataAnnotations;

namespace Huellitas.Models
{
    public class Consulta
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una mascota")]
        public int MascotaId { get; set; }

        // Una consulta puede existir sin una cita previa.
        public int? CitaId { get; set; }

        [Required(ErrorMessage = "La fecha y hora son obligatorias")]
        public DateTime FechaHora { get; set; }

        [Required(ErrorMessage = "El motivo es obligatorio")]
        [StringLength(250)]
        public string Motivo { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Sintomas { get; set; }

        [Required(ErrorMessage = "El diagnóstico es obligatorio")]
        [StringLength(1500)]
        public string Diagnostico { get; set; } = string.Empty;

        [StringLength(1500)]
        public string? Tratamiento { get; set; }

        [StringLength(1500)]
        public string? Observaciones { get; set; }

        // Relaciones con otras entidades.
        public Mascota? Mascota { get; set; }

        public Cita? Cita { get; set; }
    }
}
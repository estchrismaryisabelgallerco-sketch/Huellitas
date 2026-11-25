
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Huellitas.Models
{
    public class Personal
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe asociar una cuenta de usuario.")]
        public string IdentityUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(IdentityUserId))]
        public IdentityUser? Usuario { get; set; }

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [StringLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [StringLength(20)]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Especialidad { get; set; }

        [DataType(DataType.Date)]
        public DateTime FechaContratacion { get; set; } = DateTime.Today;

        [Required]
        [StringLength(20)]
        public string EstadoLaboral { get; set; } = "Activo";

        public ICollection<HorarioPersonal> Horarios { get; set; }
            = new List<HorarioPersonal>();

        public ICollection<Cita> Citas { get; set; }
            = new List<Cita>();
    }
}
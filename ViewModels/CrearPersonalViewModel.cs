
using System.ComponentModel.DataAnnotations;

namespace Huellitas.ViewModels
{
    public class CrearPersonalViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar una cuenta de usuario.")]
        public string IdentityUserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar el tipo de personal.")]
        [Display(Name = "Tipo de personal")]
        public string Rol { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [StringLength(100)]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(100)]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [StringLength(20)]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Especialidad")]
        public string? Especialidad { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de contratación")]
        public DateTime FechaContratacion { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Estado laboral")]
        public string EstadoLaboral { get; set; } = "Activo";
    }
}
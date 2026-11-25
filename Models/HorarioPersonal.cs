
using System.ComponentModel.DataAnnotations;

namespace Huellitas.Models
{
    public class HorarioPersonal
    {
        public int Id { get; set; }

        [Required]
        public int PersonalId { get; set; }

        public Personal? Personal { get; set; }

        [Required]
        public DayOfWeek DiaSemana { get; set; }

        [Required]
        public TimeSpan HoraEntrada { get; set; }

        [Required]
        public TimeSpan HoraSalida { get; set; }

        [Display(Name = "Número de consultorio")]
        [Required(ErrorMessage = "El número de consultorio es obligatorio.")]
        [Range(1, 3, ErrorMessage = "El número de consultorio debe ser 1, 2 o 3.")]
        public int NumeroConsultorio { get; set; }
    }
}
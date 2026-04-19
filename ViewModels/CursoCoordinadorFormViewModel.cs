using System.ComponentModel.DataAnnotations;

namespace Practica2.ViewModels;

public class CursoCoordinadorFormViewModel : IValidatableObject
{
    public int? Id { get; set; }

    [Required]
    [StringLength(20)]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "No se aceptan creditos negativos.")]
    public int Creditos { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El cupo maximo debe ser mayor a 0.")]
    public int CupoMaximo { get; set; }

    public TimeOnly HorarioInicio { get; set; }

    public TimeOnly HorarioFin { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (HorarioFin < HorarioInicio)
        {
            yield return new ValidationResult(
                "No se permite HorarioFin anterior a HorarioInicio.",
                new[] { nameof(HorarioInicio), nameof(HorarioFin) });
        }
    }
}
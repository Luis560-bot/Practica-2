using System.ComponentModel.DataAnnotations;
using Practica2.Models;

namespace Practica2.ViewModels;

public class CatalogoCursosFiltroViewModel : IValidatableObject
{
    public string? Nombre { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "No se permiten creditos negativos.")]
    public int? CreditosMin { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "No se permiten creditos negativos.")]
    public int? CreditosMax { get; set; }

    public TimeOnly? HorarioInicio { get; set; }

    public TimeOnly? HorarioFin { get; set; }

    public List<Curso> Cursos { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CreditosMin.HasValue && CreditosMax.HasValue && CreditosMin > CreditosMax)
        {
            yield return new ValidationResult(
                "El rango de creditos es invalido: el minimo no puede ser mayor al maximo.",
                new[] { nameof(CreditosMin), nameof(CreditosMax) });
        }

        if (HorarioInicio.HasValue && HorarioFin.HasValue && HorarioFin < HorarioInicio)
        {
            yield return new ValidationResult(
                "No se permite HorarioFin anterior a HorarioInicio.",
                new[] { nameof(HorarioInicio), nameof(HorarioFin) });
        }
    }
}

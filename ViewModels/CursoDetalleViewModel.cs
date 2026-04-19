using Practica2.Models;

namespace Practica2.ViewModels;

public class CursoDetalleViewModel
{
    public required Curso Curso { get; set; }

    public bool YaInscrito { get; set; }

    public int CuposDisponibles { get; set; }
}

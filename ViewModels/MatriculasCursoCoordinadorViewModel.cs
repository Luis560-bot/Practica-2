using Practica2.Models;

namespace Practica2.ViewModels;

public class MatriculasCursoCoordinadorViewModel
{
    public int CursoId { get; set; }

    public string CursoCodigo { get; set; } = string.Empty;

    public string CursoNombre { get; set; } = string.Empty;

    public List<MatriculaCoordinadorItemViewModel> Matriculas { get; set; } = new();
}

public class MatriculaCoordinadorItemViewModel
{
    public int MatriculaId { get; set; }

    public string UsuarioId { get; set; } = string.Empty;

    public string UsuarioEmail { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; }

    public EstadoMatricula Estado { get; set; }
}
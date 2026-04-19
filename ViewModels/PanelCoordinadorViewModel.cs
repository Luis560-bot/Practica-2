namespace Practica2.ViewModels;

public class PanelCoordinadorViewModel
{
    public List<CursoResumenCoordinadorViewModel> Cursos { get; set; } = new();
}

public class CursoResumenCoordinadorViewModel
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public int Creditos { get; set; }

    public int CupoMaximo { get; set; }

    public TimeOnly HorarioInicio { get; set; }

    public TimeOnly HorarioFin { get; set; }

    public bool Activo { get; set; }

    public int TotalMatriculasVigentes { get; set; }
}
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Practica2.Models;

public class Matricula
{
    public int Id { get; set; }

    [Required]
    public int CursoId { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public EstadoMatricula Estado { get; set; } = EstadoMatricula.Pendiente;

    public Curso? Curso { get; set; }

    public IdentityUser? Usuario { get; set; }
}

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Practica2.Models;

namespace Practica2.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Curso>(entity =>
        {
            entity.HasIndex(c => c.Codigo).IsUnique();
            entity.Property(c => c.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(c => c.Nombre).HasMaxLength(150).IsRequired();
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Curso_Creditos", "\"Creditos\" >= 0");
                t.HasCheckConstraint("CK_Curso_Horario", "\"HorarioInicio\" <= \"HorarioFin\"");
            });
        });

        builder.Entity<Matricula>(entity =>
        {
            entity.HasIndex(m => new { m.CursoId, m.UsuarioId }).IsUnique();

            entity
                .HasOne(m => m.Curso)
                .WithMany(c => c.Matriculas)
                .HasForeignKey(m => m.CursoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(m => m.Usuario)
                .WithMany()
                .HasForeignKey(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override int SaveChanges()
    {
        ValidarCupos();
        ValidarSolapamientos();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ValidarCupos();
        ValidarSolapamientos();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ValidarCupos()
    {
        var nuevasMatriculas = ChangeTracker
            .Entries<Matricula>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        foreach (var matricula in nuevasMatriculas)
        {
            var curso = Cursos.FirstOrDefault(c => c.Id == matricula.CursoId);
            if (curso is null)
            {
                continue;
            }

            var totalVigentes = Matriculas.Count(m =>
                m.CursoId == matricula.CursoId &&
                m.Estado != EstadoMatricula.Cancelada);

            if (totalVigentes >= curso.CupoMaximo)
            {
                throw new InvalidOperationException($"No hay cupos disponibles para el curso {curso.Codigo}.");
            }
        }
    }

    private void ValidarSolapamientos()
    {
        var nuevasMatriculas = ChangeTracker
            .Entries<Matricula>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        foreach (var matricula in nuevasMatriculas)
        {
            var cursoNuevo = Cursos.AsNoTracking().FirstOrDefault(c => c.Id == matricula.CursoId);
            if (cursoNuevo is null)
            {
                continue;
            }

            var cursoConflicto = Matriculas
                .AsNoTracking()
                .Where(m =>
                    m.UsuarioId == matricula.UsuarioId &&
                    m.Estado != EstadoMatricula.Cancelada &&
                    m.CursoId != matricula.CursoId)
                .Join(
                    Cursos.AsNoTracking(),
                    matriculaExistente => matriculaExistente.CursoId,
                    cursoExistente => cursoExistente.Id,
                    (_, cursoExistente) => cursoExistente)
                .FirstOrDefault(cursoExistente =>
                    cursoExistente.HorarioInicio < cursoNuevo.HorarioFin &&
                    cursoNuevo.HorarioInicio < cursoExistente.HorarioFin);

            if (cursoConflicto is not null)
            {
                throw new InvalidOperationException(
                    $"No se puede matricular en {cursoNuevo.Codigo} porque se solapa con el curso {cursoConflicto.Codigo}.");
            }
        }
    }
}

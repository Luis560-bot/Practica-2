using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using Practica2.Data;
using Practica2.Infrastructure;
using Practica2.Models;
using Practica2.ViewModels;
using System.Text.Json;

namespace Practica2.Controllers;

public class CatalogoCursosController : Controller
{
    private static readonly DistributedCacheEntryOptions CursosActivosCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
    };

    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IDistributedCache _cache;

    public CatalogoCursosController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IDistributedCache cache)
    {
        _context = context;
        _userManager = userManager;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] CatalogoCursosFiltroViewModel filtros)
    {
        var cursosActivos = await ObtenerCursosActivosCacheadosAsync();

        var query = cursosActivos.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filtros.Nombre))
        {
            var nombre = filtros.Nombre.Trim();
            query = query.Where(c => c.Nombre.Contains(nombre));
        }

        if (filtros.CreditosMin.HasValue)
        {
            query = query.Where(c => c.Creditos >= filtros.CreditosMin.Value);
        }

        if (filtros.CreditosMax.HasValue)
        {
            query = query.Where(c => c.Creditos <= filtros.CreditosMax.Value);
        }

        if (filtros.HorarioInicio.HasValue)
        {
            query = query.Where(c => c.HorarioInicio >= filtros.HorarioInicio.Value);
        }

        if (filtros.HorarioFin.HasValue)
        {
            query = query.Where(c => c.HorarioFin <= filtros.HorarioFin.Value);
        }

        filtros.Cursos = query.ToList();
        return View(filtros);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var curso = await _context.Cursos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.Activo);

        if (curso is null)
        {
            return NotFound();
        }

        HttpContext.Session.SetInt32(AppStateKeys.UltimoCursoIdSessionKey, curso.Id);
        HttpContext.Session.SetString(AppStateKeys.UltimoCursoNombreSessionKey, curso.Nombre);

        var yaInscrito = false;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                yaInscrito = await _context.Matriculas
                    .AsNoTracking()
                    .AnyAsync(m =>
                        m.CursoId == curso.Id &&
                        m.UsuarioId == userId &&
                        m.Estado != EstadoMatricula.Cancelada);
            }
        }

        var totalVigentes = await _context.Matriculas
            .AsNoTracking()
            .CountAsync(m => m.CursoId == curso.Id && m.Estado != EstadoMatricula.Cancelada);

        var model = new CursoDetalleViewModel
        {
            Curso = curso,
            YaInscrito = yaInscrito,
            CuposDisponibles = Math.Max(curso.CupoMaximo - totalVigentes, 0)
        };

        return View(model);
    }

    private async Task<List<Curso>> ObtenerCursosActivosCacheadosAsync()
    {
        var cursosCacheados = await _cache.GetStringAsync(AppStateKeys.CursosActivosCacheKey);
        if (!string.IsNullOrWhiteSpace(cursosCacheados))
        {
            var cursosDesdeCache = JsonSerializer.Deserialize<List<Curso>>(cursosCacheados);
            if (cursosDesdeCache is not null)
            {
                return cursosDesdeCache;
            }
        }

        var cursosActivos = await _context.Cursos
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        var serializado = JsonSerializer.Serialize(cursosActivos);
        await _cache.SetStringAsync(AppStateKeys.CursosActivosCacheKey, serializado, CursosActivosCacheOptions);

        return cursosActivos;
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inscribirse(int id)
    {
        var curso = await _context.Cursos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.Activo);

        if (curso is null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var matriculaExistente = await _context.Matriculas
            .AsNoTracking()
            .Where(m =>
                m.UsuarioId == userId &&
                m.Estado != EstadoMatricula.Cancelada &&
                m.CursoId != id)
            .Join(
                _context.Cursos.AsNoTracking(),
                matricula => matricula.CursoId,
                cursoExistente => cursoExistente.Id,
                (_, cursoExistente) => cursoExistente)
            .FirstOrDefaultAsync(cursoExistente =>
                cursoExistente.HorarioInicio < curso.HorarioFin &&
                curso.HorarioInicio < cursoExistente.HorarioFin);

        if (matriculaExistente is not null)
        {
            TempData["ErrorInscripcion"] =
                $"No puedes inscribirte porque el horario se solapa con el curso {matriculaExistente.Codigo}.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        var yaInscrito = await _context.Matriculas
            .AsNoTracking()
            .AnyAsync(m =>
                m.CursoId == id &&
                m.UsuarioId == userId &&
                m.Estado != EstadoMatricula.Cancelada);

        if (yaInscrito)
        {
            TempData["ErrorInscripcion"] = "Ya estas inscrito en este curso.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        _context.Matriculas.Add(new Matricula
        {
            CursoId = id,
            UsuarioId = userId,
            Estado = EstadoMatricula.Pendiente,
            FechaRegistro = DateTime.UtcNow
        });

        try
        {
            await _context.SaveChangesAsync();
            TempData["ExitoInscripcion"] = "Inscripcion registrada en estado Pendiente.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorInscripcion"] = ex.Message;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorInscripcion"] = "No fue posible registrar la inscripcion.";
        }

        return RedirectToAction(nameof(Detalle), new { id });
    }
}

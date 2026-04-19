using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using Practica2.Data;
using Practica2.Infrastructure;
using Practica2.Models;
using Practica2.ViewModels;

namespace Practica2.Controllers;

[Authorize(Roles = "Coordinador")]
public class CoordinadorController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IDistributedCache _cache;

    public CoordinadorController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IDistributedCache cache)
    {
        _context = context;
        _userManager = userManager;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cursos = await _context.Cursos
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CursoResumenCoordinadorViewModel
            {
                Id = c.Id,
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                Creditos = c.Creditos,
                CupoMaximo = c.CupoMaximo,
                HorarioInicio = c.HorarioInicio,
                HorarioFin = c.HorarioFin,
                Activo = c.Activo,
                TotalMatriculasVigentes = c.Matriculas.Count(m => m.Estado != EstadoMatricula.Cancelada)
            })
            .ToListAsync();

        return View(new PanelCoordinadorViewModel { Cursos = cursos });
    }

    [HttpGet]
    public IActionResult CrearCurso()
    {
        return View(new CursoCoordinadorFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCurso(CursoCoordinadorFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var codigoExiste = await _context.Cursos
            .AsNoTracking()
            .AnyAsync(c => c.Codigo == model.Codigo);

        if (codigoExiste)
        {
            ModelState.AddModelError(nameof(model.Codigo), "Ya existe un curso con ese codigo.");
            return View(model);
        }

        _context.Cursos.Add(new Curso
        {
            Codigo = model.Codigo.Trim(),
            Nombre = model.Nombre.Trim(),
            Creditos = model.Creditos,
            CupoMaximo = model.CupoMaximo,
            HorarioInicio = model.HorarioInicio,
            HorarioFin = model.HorarioFin,
            Activo = true
        });

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(AppStateKeys.CursosActivosCacheKey);
        TempData["ExitoCoordinador"] = "Curso creado correctamente.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditarCurso(int id)
    {
        var curso = await _context.Cursos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (curso is null)
        {
            return NotFound();
        }

        var model = new CursoCoordinadorFormViewModel
        {
            Id = curso.Id,
            Codigo = curso.Codigo,
            Nombre = curso.Nombre,
            Creditos = curso.Creditos,
            CupoMaximo = curso.CupoMaximo,
            HorarioInicio = curso.HorarioInicio,
            HorarioFin = curso.HorarioFin
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCurso(CursoCoordinadorFormViewModel model)
    {
        if (!model.Id.HasValue)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var curso = await _context.Cursos.FirstOrDefaultAsync(c => c.Id == model.Id.Value);
        if (curso is null)
        {
            return NotFound();
        }

        var codigoExiste = await _context.Cursos
            .AsNoTracking()
            .AnyAsync(c => c.Codigo == model.Codigo && c.Id != curso.Id);

        if (codigoExiste)
        {
            ModelState.AddModelError(nameof(model.Codigo), "Ya existe un curso con ese codigo.");
            return View(model);
        }

        curso.Codigo = model.Codigo.Trim();
        curso.Nombre = model.Nombre.Trim();
        curso.Creditos = model.Creditos;
        curso.CupoMaximo = model.CupoMaximo;
        curso.HorarioInicio = model.HorarioInicio;
        curso.HorarioFin = model.HorarioFin;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(AppStateKeys.CursosActivosCacheKey);
        TempData["ExitoCoordinador"] = "Curso actualizado correctamente.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DesactivarCurso(int id)
    {
        var curso = await _context.Cursos.FirstOrDefaultAsync(c => c.Id == id);
        if (curso is null)
        {
            return NotFound();
        }

        if (!curso.Activo)
        {
            TempData["ExitoCoordinador"] = "El curso ya estaba desactivado.";
            return RedirectToAction(nameof(Index));
        }

        curso.Activo = false;
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(AppStateKeys.CursosActivosCacheKey);
        TempData["ExitoCoordinador"] = "Curso desactivado correctamente.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> MatriculasCurso(int cursoId)
    {
        var curso = await _context.Cursos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cursoId);

        if (curso is null)
        {
            return NotFound();
        }

        var matriculas = await _context.Matriculas
            .AsNoTracking()
            .Where(m => m.CursoId == cursoId)
            .OrderByDescending(m => m.FechaRegistro)
            .ToListAsync();

        var usuarios = await _userManager.Users
            .AsNoTracking()
            .Where(u => matriculas.Select(m => m.UsuarioId).Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? string.Empty);

        var model = new MatriculasCursoCoordinadorViewModel
        {
            CursoId = curso.Id,
            CursoCodigo = curso.Codigo,
            CursoNombre = curso.Nombre,
            Matriculas = matriculas.Select(m => new MatriculaCoordinadorItemViewModel
            {
                MatriculaId = m.Id,
                UsuarioId = m.UsuarioId,
                UsuarioEmail = usuarios.TryGetValue(m.UsuarioId, out var email) ? email : "(sin correo)",
                FechaRegistro = m.FechaRegistro,
                Estado = m.Estado
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarMatricula(int id)
    {
        var matricula = await _context.Matriculas.FirstOrDefaultAsync(m => m.Id == id);
        if (matricula is null)
        {
            return NotFound();
        }

        matricula.Estado = EstadoMatricula.Confirmada;
        await _context.SaveChangesAsync();
        TempData["ExitoCoordinador"] = "Matricula confirmada.";

        return RedirectToAction(nameof(MatriculasCurso), new { cursoId = matricula.CursoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarMatricula(int id)
    {
        var matricula = await _context.Matriculas.FirstOrDefaultAsync(m => m.Id == id);
        if (matricula is null)
        {
            return NotFound();
        }

        matricula.Estado = EstadoMatricula.Cancelada;
        await _context.SaveChangesAsync();
        TempData["ExitoCoordinador"] = "Matricula cancelada.";

        return RedirectToAction(nameof(MatriculasCurso), new { cursoId = matricula.CursoId });
    }
}
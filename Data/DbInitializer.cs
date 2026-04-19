using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Practica2.Models;

namespace Practica2.Data;

public static class DbInitializer
{
    private const string CoordinadorRole = "Coordinador";
    private const string CoordinadorEmail = "coordinador@universidad.local";
    private const string CoordinadorPassword = "Coord!2026";

    public static async Task InicializarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await context.Database.MigrateAsync();

        if (!await roleManager.RoleExistsAsync(CoordinadorRole))
        {
            await roleManager.CreateAsync(new IdentityRole(CoordinadorRole));
        }

        var coordinador = await userManager.FindByEmailAsync(CoordinadorEmail);
        if (coordinador is null)
        {
            coordinador = new IdentityUser
            {
                UserName = CoordinadorEmail,
                Email = CoordinadorEmail,
                EmailConfirmed = true
            };

            var resultado = await userManager.CreateAsync(coordinador, CoordinadorPassword);
            if (!resultado.Succeeded)
            {
                var errores = string.Join("; ", resultado.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"No se pudo crear usuario coordinador: {errores}");
            }
        }

        if (!await userManager.IsInRoleAsync(coordinador, CoordinadorRole))
        {
            await userManager.AddToRoleAsync(coordinador, CoordinadorRole);
        }

        if (!await context.Cursos.AnyAsync())
        {
            context.Cursos.AddRange(
                new Curso
                {
                    Codigo = "MAT101",
                    Nombre = "Calculo I",
                    Creditos = 4,
                    CupoMaximo = 30,
                    HorarioInicio = new TimeOnly(8, 0),
                    HorarioFin = new TimeOnly(10, 0),
                    Activo = true
                },
                new Curso
                {
                    Codigo = "INF220",
                    Nombre = "Estructuras de Datos",
                    Creditos = 5,
                    CupoMaximo = 25,
                    HorarioInicio = new TimeOnly(10, 30),
                    HorarioFin = new TimeOnly(12, 30),
                    Activo = true
                },
                new Curso
                {
                    Codigo = "ADM110",
                    Nombre = "Introduccion a la Administracion",
                    Creditos = 3,
                    CupoMaximo = 35,
                    HorarioInicio = new TimeOnly(14, 0),
                    HorarioFin = new TimeOnly(16, 0),
                    Activo = true
                }
            );

            await context.SaveChangesAsync();
        }
    }
}

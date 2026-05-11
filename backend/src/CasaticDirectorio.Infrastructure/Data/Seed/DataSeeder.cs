using CasaticDirectorio.Domain.Entities;
using CasaticDirectorio.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CasaticDirectorio.Infrastructure.Data.Seed;

/// <summary>
/// Inicializa datos base del sistema:
/// - Socio de prueba (sólo en primer arranque, si no hay socios)
/// - Usuario admin (credenciales desde Seed:AdminEmail / Seed:AdminPassword)
/// - Eventos de demostración
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        // ── 1. SOCIO DE PRUEBA ─────────────────────────────
        Socio? socioPrueba;
        if (!await db.Socios.AnyAsync())
        {
            socioPrueba = new Socio
            {
                Id = Guid.NewGuid(),
                NombreEmpresa = "Empresa de Prueba",
                Slug = "empresa-prueba",
                Descripcion = "Empresa de prueba para validar el sistema.",
                Especialidades = new List<string> { "Software", "Consultoría" },
                Servicios = new List<string> { "Desarrollo", "Asesoría" },
                Habilitado = true,
                EstadoFinanciero = EstadoFinanciero.AlDia
            };

            db.Socios.Add(socioPrueba);
            await db.SaveChangesAsync();
        }
        else
        {
            socioPrueba = await db.Socios.FirstAsync();
        }

        // ── 2. USUARIOS ────────────────────────────────────
        if (!await db.Usuarios.AnyAsync())
        {
            var adminEmail = config["Seed:AdminEmail"] ?? "admin@casatic.org";
            var adminPassword = config["Seed:AdminPassword"];

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "Seed:AdminPassword no está configurada. Definí SEED_ADMIN_PASSWORD en .env " +
                    "para crear el usuario administrador inicial.");
            }

            db.Usuarios.AddRange(
                new Usuario
                {
                    Id = Guid.NewGuid(),
                    Email = adminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Rol = Rol.Admin,
                    PrimerLogin = true, // ← antes false. Forzamos cambio de contraseña en primer login.
                    Activo = true,
                    SocioId = null
                },
                new Usuario
                {
                    Id = Guid.NewGuid(),
                    Email = "prueba@prueba.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Socio123!"),
                    Rol = Rol.Socio,
                    PrimerLogin = true,
                    Activo = true,
                    SocioId = socioPrueba.Id
                }
            );

            await db.SaveChangesAsync();

            logger.LogInformation(
                "Usuario admin creado: {Email}. Cambiá la contraseña en el primer login.",
                adminEmail);
        }

        // ── 3. EVENTOS DE PRUEBA ───────────────────────────
        if (!await db.Eventos.AnyAsync())
        {
            db.Eventos.AddRange(
                new Evento
                {
                    Id = Guid.NewGuid(),
                    SocioId = socioPrueba.Id,
                    Titulo = "Conferencia de Innovación CASATIC",
                    Slug = "conferencia-innovacion-casatic",
                    Descripcion = "Evento enfocado en transformación digital, innovación y tecnología empresarial.",
                    Tipo = TipoEvento.Conferencia,
                    Modalidad = ModalidadEvento.Presencial,
                    FechaInicio = DateTime.UtcNow.AddDays(10),
                    FechaFin = DateTime.UtcNow.AddDays(10).AddHours(4),
                    Lugar = "San Salvador, El Salvador",
                    ImageUrl = "",
                    Estado = EstadoEvento.Aprobado,
                    Habilitado = true,
                    Destacado = true,
                    PublicadoAt = DateTime.UtcNow
                },
                new Evento
                {
                    Id = Guid.NewGuid(),
                    SocioId = socioPrueba.Id,
                    Titulo = "Webinar de Ciberseguridad Empresarial",
                    Slug = "webinar-ciberseguridad-empresarial",
                    Descripcion = "Buenas prácticas de seguridad informática para empresas.",
                    Tipo = TipoEvento.Webinar,
                    Modalidad = ModalidadEvento.Virtual,
                    FechaInicio = DateTime.UtcNow.AddDays(5),
                    FechaFin = DateTime.UtcNow.AddDays(5).AddHours(2),
                    Lugar = "Online",
                    ImageUrl = "",
                    Estado = EstadoEvento.Aprobado,
                    Habilitado = true,
                    Destacado = false,
                    PublicadoAt = DateTime.UtcNow
                }
            );

            await db.SaveChangesAsync();
        }
    }
}

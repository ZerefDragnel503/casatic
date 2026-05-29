using CasaticDirectorio.Domain.Entities;
using CasaticDirectorio.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CasaticDirectorio.Infrastructure.Data.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        await EnsureFacturasTableAsync(db);

        Socio? socioPrueba;
        if (!await db.Socios.AnyAsync())
        {
            socioPrueba = new Socio
            {
                Id = Guid.NewGuid(),
                NombreEmpresa = "Empresa de Prueba",
                Slug = "empresa-prueba",
                Descripcion = "Empresa de prueba para validar el sistema.",
                Especialidades = new List<string> { "Software", "Consultoria" },
                Servicios = new List<string> { "Desarrollo", "Asesoria" },
                Habilitado = true,
                EstadoFinanciero = EstadoFinanciero.AlDia
            };

            db.Socios.Add(socioPrueba);
            await db.SaveChangesAsync();
        }
        else
        {
            socioPrueba = await db.Socios
                .OrderBy(s => s.NombreEmpresa)
                .FirstAsync();
        }

        if (!await db.Usuarios.AnyAsync())
        {
            var adminEmail = config["Seed:AdminEmail"] ?? "admin@casatic.org";
            var adminPassword = config["Seed:AdminPassword"];

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "Seed:AdminPassword no esta configurada. Defini SEED_ADMIN_PASSWORD en .env " +
                    "para crear el usuario administrador inicial.");
            }

            db.Usuarios.AddRange(
                new Usuario
                {
                    Id = Guid.NewGuid(),
                    Email = adminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Rol = Rol.Admin,
                    PrimerLogin = true,
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
                "Usuario admin creado: {Email}. Cambia la contrasena en el primer login.",
                adminEmail);
        }

        if (!await db.Eventos.AnyAsync())
        {
            db.Eventos.AddRange(
                new Evento
                {
                    Id = Guid.NewGuid(),
                    SocioId = socioPrueba.Id,
                    Titulo = "Conferencia de Innovacion CASATIC",
                    Slug = "conferencia-innovacion-casatic",
                    Descripcion = "Evento enfocado en transformacion digital, innovacion y tecnologia empresarial.",
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
                    Descripcion = "Buenas practicas de seguridad informatica para empresas.",
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

        await EnsureFacturasParaSociosAsync(db, logger);
    }

    private static async Task EnsureFacturasTableAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS facturas (
                "Id" UUID NOT NULL DEFAULT gen_random_uuid(),
                "SocioId" UUID NOT NULL,
                "Numero" VARCHAR(40) NOT NULL,
                "TipoDocumento" VARCHAR(60) NOT NULL DEFAULT 'Factura interna',
                "CodigoGeneracion" VARCHAR(40) NOT NULL DEFAULT '',
                "NumeroControl" VARCHAR(60) NOT NULL DEFAULT '',
                "SelloRecepcion" VARCHAR(120) NOT NULL DEFAULT '',
                "Ambiente" VARCHAR(30) NOT NULL DEFAULT 'Produccion',
                "CondicionOperacion" VARCHAR(30) NOT NULL DEFAULT 'Credito',
                "FormaPago" VARCHAR(60) NOT NULL DEFAULT 'Transferencia',
                "ReferenciaPago" VARCHAR(120) NOT NULL DEFAULT '',
                "PlanNombre" VARCHAR(120) NOT NULL,
                "PlanPeriodo" VARCHAR(40) NOT NULL,
                "Descripcion" TEXT NOT NULL,
                "Subtotal" NUMERIC(12,2) NOT NULL,
                "Iva" NUMERIC(12,2) NOT NULL,
                "Total" NUMERIC(12,2) NOT NULL,
                "Estado" VARCHAR(20) NOT NULL DEFAULT 'Pendiente',
                "FechaEmision" TIMESTAMPTZ NOT NULL DEFAULT now(),
                "FechaVencimiento" TIMESTAMPTZ NOT NULL,
                "FechaPago" TIMESTAMPTZ DEFAULT NULL,
                "Notas" TEXT NOT NULL DEFAULT '',
                "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT now(),
                "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT now(),
                CONSTRAINT pk_facturas PRIMARY KEY ("Id"),
                CONSTRAINT fk_facturas_socios FOREIGN KEY ("SocioId")
                    REFERENCES socios("Id") ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ix_facturas_numero ON facturas("Numero");
            CREATE UNIQUE INDEX IF NOT EXISTS ix_facturas_socio_id ON facturas("SocioId");
            CREATE INDEX IF NOT EXISTS ix_facturas_estado ON facturas("Estado");
            CREATE INDEX IF NOT EXISTS ix_facturas_fecha_vencimiento ON facturas("FechaVencimiento");
            """);

        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE facturas ADD COLUMN IF NOT EXISTS "TipoDocumento" VARCHAR(60) NOT NULL DEFAULT 'Factura interna';
            ALTER TABLE facturas ADD COLUMN IF NOT EXISTS "CodigoGeneracion" VARCHAR(40) NOT NULL DEFAULT '';
            ALTER TABLE facturas ADD COLUMN IF NOT EXISTS "NumeroControl" VARCHAR(60) NOT NULL DEFAULT '';
            ALTER TABLE facturas ADD COLUMN IF NOT EXISTS "SelloRecepcion" VARCHAR(120) NOT NULL DEFAULT '';
            ALTER TABLE facturas ADD COLUMN IF NOT EXISTS "Ambiente" VARCHAR(30) NOT NULL DEFAULT 'Produccion';
            ALTER TABLE facturas ADD COLUMN IF NOT EXISTS "CondicionOperacion" VARCHAR(30) NOT NULL DEFAULT 'Credito';
            ALTER TABLE facturas ADD COLUMN IF NOT EXISTS "FormaPago" VARCHAR(60) NOT NULL DEFAULT 'Transferencia';
            ALTER TABLE facturas ADD COLUMN IF NOT EXISTS "ReferenciaPago" VARCHAR(120) NOT NULL DEFAULT '';
            UPDATE facturas
            SET "CodigoGeneracion" = lower("Id"::text)
            WHERE coalesce("CodigoGeneracion", '') = '';
            UPDATE facturas
            SET "NumeroControl" = 'DTE-01-CASATIC-' || replace("Numero", 'CAS-', '')
            WHERE coalesce("NumeroControl", '') = '';
            """);
    }

    private static async Task EnsureFacturasParaSociosAsync(AppDbContext db, ILogger logger)
    {
        var sociosSinFactura = await db.Socios
            .Where(s => !db.Facturas.Any(f => f.SocioId == s.Id))
            .OrderBy(s => s.NombreEmpresa)
            .ToListAsync();

        if (sociosSinFactura.Count == 0)
            return;

        var year = DateTime.UtcNow.Year;
        var lastNumero = await db.Facturas
            .Where(f => f.FechaEmision.Year == year && EF.Functions.ILike(f.Numero, $"CAS-{year}-%"))
            .OrderByDescending(f => f.Numero)
            .Select(f => f.Numero)
            .FirstOrDefaultAsync();

        var correlativo = 1;
        if (!string.IsNullOrWhiteSpace(lastNumero))
        {
            var match = Regex.Match(lastNumero, $"^CAS-{year}-(\\d{{4}})$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var valor))
            {
                correlativo = valor + 1;
            }
        }

        foreach (var socio in sociosSinFactura)
        {
            const decimal subtotal = 400m;
            var iva = Math.Round(subtotal * 0.13m, 2);

            db.Facturas.Add(new Factura
            {
                Id = Guid.NewGuid(),
                SocioId = socio.Id,
                Numero = $"CAS-{year}-{correlativo:0000}",
                CodigoGeneracion = Guid.NewGuid().ToString().ToUpperInvariant(),
                NumeroControl = $"DTE-01-CASATIC-{year}-{correlativo:0000}",
                PlanNombre = "Socios Miembros",
                PlanPeriodo = "anual",
                Descripcion = "Membresia anual CASATIC - Socios Miembros",
                Subtotal = subtotal,
                Iva = iva,
                Total = subtotal + iva,
                Estado = EstadoFactura.Pendiente,
                FechaEmision = DateTime.UtcNow,
                FechaVencimiento = DateTime.UtcNow.AddDays(30),
                Notas = "Factura generada automaticamente desde el plan de membresia publicado en el home."
            });

            correlativo++;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Facturas iniciales generadas: {Count}", sociosSinFactura.Count);
    }
}

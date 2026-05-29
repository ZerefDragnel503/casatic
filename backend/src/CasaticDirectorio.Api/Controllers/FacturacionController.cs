using System.Globalization;
using System.Net;
using System.Text;
using CasaticDirectorio.Api.DTOs.Facturacion;
using CasaticDirectorio.Domain.Entities;
using CasaticDirectorio.Domain.Enums;
using CasaticDirectorio.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CasaticDirectorio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FacturacionController : ControllerBase
{
    private const decimal IvaRate = 0.13m;
    private readonly AppDbContext _db;

    public FacturacionController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("planes")]
    public IActionResult GetPlanes() => Ok(Planes);

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        await EnsureFacturasParaSociosAsync();

        var facturas = await _db.Facturas
            .Include(f => f.Socio)
            .OrderBy(f => f.Socio.NombreEmpresa)
            .ToListAsync();

        return Ok(facturas.Select(ToDto));
    }

    [HttpPost("generar-todas")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerarTodas()
    {
        var creadas = await EnsureFacturasParaSociosAsync();
        return Ok(new { creadas });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Upsert([FromBody] FacturaUpsertDto dto)
    {
        var socio = await _db.Socios.FindAsync(dto.SocioId);
        if (socio == null) return NotFound(new { message = "Socio no encontrado." });

        var factura = await _db.Facturas.FirstOrDefaultAsync(f => f.SocioId == dto.SocioId);
        if (factura == null)
        {
            var numero = await NextNumeroAsync();
            factura = new Factura
            {
                Id = Guid.NewGuid(),
                SocioId = dto.SocioId,
                Numero = numero,
                CodigoGeneracion = Guid.NewGuid().ToString().ToUpperInvariant(),
                NumeroControl = BuildNumeroControl(numero)
            };
            _db.Facturas.Add(factura);
        }

        Apply(factura, dto);
        await _db.SaveChangesAsync();

        factura.Socio = socio;
        return Ok(ToDto(factura));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] FacturaUpdateDto dto)
    {
        var factura = await _db.Facturas.Include(f => f.Socio).FirstOrDefaultAsync(f => f.Id == id);
        if (factura == null) return NotFound();

        Apply(factura, dto);
        await _db.SaveChangesAsync();

        return Ok(ToDto(factura));
    }

    [HttpGet("{id:guid}/descargar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Descargar(Guid id)
    {
        var factura = await _db.Facturas.Include(f => f.Socio).FirstOrDefaultAsync(f => f.Id == id);
        if (factura == null) return NotFound();

        return HtmlFactura(factura);
    }

    [HttpGet("mi-factura")]
    [Authorize(Roles = "Socio")]
    public async Task<IActionResult> MiFactura()
    {
        var socioId = GetSocioId();
        if (socioId == null) return Forbid();

        var factura = await _db.Facturas
            .Include(f => f.Socio)
            .FirstOrDefaultAsync(f => f.SocioId == socioId.Value);

        if (factura == null) return NotFound(new { message = "Tu empresa aun no tiene factura asignada." });
        return Ok(ToDto(factura));
    }

    [HttpGet("mi-factura/descargar")]
    [Authorize(Roles = "Socio")]
    public async Task<IActionResult> DescargarMiFactura()
    {
        var socioId = GetSocioId();
        if (socioId == null) return Forbid();

        var factura = await _db.Facturas
            .Include(f => f.Socio)
            .FirstOrDefaultAsync(f => f.SocioId == socioId.Value);

        if (factura == null) return NotFound();
        return HtmlFactura(factura);
    }

    private async Task<int> EnsureFacturasParaSociosAsync()
    {
        var sociosSinFactura = await _db.Socios
            .Where(s => !_db.Facturas.Any(f => f.SocioId == s.Id))
            .OrderBy(s => s.NombreEmpresa)
            .ToListAsync();

        if (sociosSinFactura.Count == 0)
            return 0;

        var year = DateTime.UtcNow.Year;
        var correlativo = await _db.Facturas.CountAsync(f => f.FechaEmision.Year == year) + 1;

        foreach (var socio in sociosSinFactura)
        {
            var subtotal = DefaultPlan.MontoSugerido;
            var iva = Math.Round(subtotal * IvaRate, 2);

            _db.Facturas.Add(new Factura
            {
                Id = Guid.NewGuid(),
                SocioId = socio.Id,
                Numero = $"CAS-{year}-{correlativo:0000}",
                CodigoGeneracion = Guid.NewGuid().ToString().ToUpperInvariant(),
                NumeroControl = $"DTE-01-CASATIC-{year}-{correlativo:0000}",
                PlanNombre = DefaultPlan.Nombre,
                PlanPeriodo = DefaultPlan.Periodo,
                Descripcion = DefaultPlan.Descripcion,
                Subtotal = subtotal,
                Iva = iva,
                Total = subtotal + iva,
                Estado = EstadoFactura.Pendiente,
                FechaEmision = DateTime.UtcNow,
                FechaVencimiento = DateTime.UtcNow.AddDays(30),
                Notas = "Factura generada desde los planes de membresia publicados en el home."
            });

            correlativo++;
        }

        await _db.SaveChangesAsync();
        return sociosSinFactura.Count;
    }

    private async Task<string> NextNumeroAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Facturas.CountAsync(f => f.FechaEmision.Year == year);
        return $"CAS-{year}-{count + 1:0000}";
    }

    private static void Apply(Factura factura, FacturaUpsertDto dto)
    {
        Apply(factura, new FacturaUpdateDto
        {
            PlanNombre = dto.PlanNombre,
            PlanPeriodo = dto.PlanPeriodo,
            Descripcion = dto.Descripcion,
            Subtotal = dto.Subtotal,
            Estado = dto.Estado,
            FechaEmision = dto.FechaEmision ?? DateTime.UtcNow,
            FechaVencimiento = dto.FechaVencimiento ?? DateTime.UtcNow.AddDays(30),
            FechaPago = dto.FechaPago,
            TipoDocumento = dto.TipoDocumento,
            CodigoGeneracion = dto.CodigoGeneracion,
            NumeroControl = dto.NumeroControl,
            SelloRecepcion = dto.SelloRecepcion,
            Ambiente = dto.Ambiente,
            CondicionOperacion = dto.CondicionOperacion,
            FormaPago = dto.FormaPago,
            ReferenciaPago = dto.ReferenciaPago,
            Notas = dto.Notas
        });
    }

    private static void Apply(Factura factura, FacturaUpdateDto dto)
    {
        if (!Enum.TryParse<EstadoFactura>(dto.Estado, true, out var estado))
            estado = EstadoFactura.Pendiente;

        var subtotal = Math.Max(0, dto.Subtotal);
        var iva = Math.Round(subtotal * IvaRate, 2);

        factura.PlanNombre = dto.PlanNombre.Trim();
        factura.PlanPeriodo = dto.PlanPeriodo.Trim();
        factura.Descripcion = dto.Descripcion.Trim();
        factura.Subtotal = subtotal;
        factura.Iva = iva;
        factura.Total = subtotal + iva;
        factura.Estado = estado;
        factura.TipoDocumento = Clean(dto.TipoDocumento, "Factura interna");
        factura.CodigoGeneracion = Clean(dto.CodigoGeneracion, factura.CodigoGeneracion);
        if (string.IsNullOrWhiteSpace(factura.CodigoGeneracion))
            factura.CodigoGeneracion = Guid.NewGuid().ToString().ToUpperInvariant();
        factura.NumeroControl = Clean(dto.NumeroControl, factura.NumeroControl);
        if (string.IsNullOrWhiteSpace(factura.NumeroControl))
            factura.NumeroControl = BuildNumeroControl(factura.Numero);
        factura.SelloRecepcion = Clean(dto.SelloRecepcion, "");
        factura.Ambiente = Clean(dto.Ambiente, "Produccion");
        factura.CondicionOperacion = Clean(dto.CondicionOperacion, "Credito");
        factura.FormaPago = Clean(dto.FormaPago, "Transferencia");
        factura.ReferenciaPago = Clean(dto.ReferenciaPago, "");
        factura.FechaEmision = EnsureUtc(dto.FechaEmision);
        factura.FechaVencimiento = EnsureUtc(dto.FechaVencimiento);
        factura.FechaPago = dto.FechaPago.HasValue ? EnsureUtc(dto.FechaPago.Value) : null;
        factura.Notas = dto.Notas.Trim();
        factura.UpdatedAt = DateTime.UtcNow;
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static string Clean(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string BuildNumeroControl(string numero) =>
        $"DTE-01-CASATIC-{numero.Replace("CAS-", "", StringComparison.OrdinalIgnoreCase)}";

    private Guid? GetSocioId()
    {
        var value = User.FindFirst("SocioId")?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private FileContentResult HtmlFactura(Factura factura)
    {
        var html = BuildHtml(factura);
        var fileName = $"Factura-{factura.Numero}.html";
        return File(Encoding.UTF8.GetBytes(html), "text/html; charset=utf-8", fileName);
    }

    private static FacturaDto ToDto(Factura f) => new()
    {
        Id = f.Id,
        SocioId = f.SocioId,
        SocioNombre = f.Socio?.NombreEmpresa ?? "",
        Numero = f.Numero,
        TipoDocumento = f.TipoDocumento,
        CodigoGeneracion = f.CodigoGeneracion,
        NumeroControl = f.NumeroControl,
        SelloRecepcion = f.SelloRecepcion,
        Ambiente = f.Ambiente,
        CondicionOperacion = f.CondicionOperacion,
        FormaPago = f.FormaPago,
        ReferenciaPago = f.ReferenciaPago,
        PlanNombre = f.PlanNombre,
        PlanPeriodo = f.PlanPeriodo,
        Descripcion = f.Descripcion,
        Subtotal = f.Subtotal,
        Iva = f.Iva,
        Total = f.Total,
        Estado = f.Estado.ToString(),
        FechaEmision = f.FechaEmision,
        FechaVencimiento = f.FechaVencimiento,
        FechaPago = f.FechaPago,
        Notas = f.Notas
    };

    private static string BuildHtml(Factura f)
    {
        static string H(string value) => WebUtility.HtmlEncode(value ?? "");
        static string Money(decimal value) => value.ToString("C", CultureInfo.GetCultureInfo("en-US"));
        static string Date(DateTime value) => value.ToString("yyyy-MM-dd");
        var tieneSello = !string.IsNullOrWhiteSpace(f.SelloRecepcion);
        var estadoFiscal = tieneSello
            ? "DTE recibido por Hacienda"
            : "Documento interno pendiente de firma, transmision y sello de Hacienda";
        var verificacion = tieneSello
            ? $"https://factura.gob.sv/consulta-publica?codigoGeneracion={WebUtility.UrlEncode(f.CodigoGeneracion)}"
            : "https://factura.gob.sv/";
        var avisoFiscal = tieneSello
            ? ""
            : """
              <div class="notice">
                Esta representacion es una factura interna CASATIC. Para tener validez como Documento Tributario Electronico debe ser generada, firmada, transmitida a Hacienda y contar con sello de recepcion.
              </div>
              """;

        return $$$"""
            <!doctype html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <title>Factura {{{H(f.Numero)}}}</title>
              <style>
                *{box-sizing:border-box}body{font-family:Arial,sans-serif;margin:0;background:#eef3f8;color:#172033}
                .page{max-width:920px;margin:28px auto;background:#fff;box-shadow:0 18px 45px rgba(15,23,42,.12)}
                .bar{height:8px;background:#0c9ec6}.inner{padding:38px}.top{display:flex;justify-content:space-between;gap:28px;border-bottom:1px solid #d8e0ea;padding-bottom:28px}
                h1{margin:0;color:#0e3877;font-size:34px}.muted{color:#667085}.small{font-size:12px}.right{text-align:right}
                .badge{display:inline-block;border:1px solid #bdd7e5;background:#eef9fd;color:#0e6178;border-radius:999px;padding:6px 10px;font-size:12px;font-weight:700}
                .grid{display:grid;grid-template-columns:1fr 1fr;gap:16px;margin:24px 0}.box{border:1px solid #d8e0ea;border-radius:8px;padding:18px}
                .label{font-size:11px;text-transform:uppercase;letter-spacing:.08em;color:#667085;font-weight:700;margin-bottom:6px}
                table{width:100%;border-collapse:collapse;margin-top:22px}th,td{padding:13px;border-bottom:1px solid #e8edf3;text-align:left;vertical-align:top}
                th{background:#f4f8fb;color:#344054;font-size:12px;text-transform:uppercase}.total{font-size:24px;font-weight:800;color:#0e3877}
                .notice{border-left:4px solid #f59e0b;background:#fffbeb;padding:14px 16px;margin:22px 0;color:#7c2d12}
                .fiscal{display:grid;grid-template-columns:1fr 1fr;gap:10px;font-size:12px}.mono{font-family:Consolas,monospace;word-break:break-all}
                button{background:#0e3877;color:#fff;border:0;border-radius:6px;padding:10px 14px;font-weight:700;margin-bottom:18px}
                @media(max-width:700px){.top,.grid,.fiscal{grid-template-columns:1fr;display:grid}.right{text-align:left}.inner{padding:22px}}
                @media print{button{display:none}body{background:#fff}.page{box-shadow:none;margin:0;max-width:none}.inner{padding:24px}}
              </style>
            </head>
            <body>
              <div class="page">
                <div class="bar"></div>
                <div class="inner">
                  <button onclick="window.print()">Imprimir / guardar como PDF</button>
                  <div class="top">
                    <div>
                      <h1>CASATIC</h1>
                      <p class="muted">Directorio Interactivo CASATIC 2026</p>
                      <span class="badge">{{{H(estadoFiscal)}}}</span>
                    </div>
                    <div class="right">
                      <h2>{{{H(f.TipoDocumento)}}}</h2>
                      <p class="mono"><strong>{{{H(f.Numero)}}}</strong></p>
                      <p>Estado: <strong>{{{H(f.Estado.ToString())}}}</strong></p>
                    </div>
                  </div>

                  <div class="grid">
                    <div class="box">
                      <div class="label">Facturar a</div>
                      <strong>{{{H(f.Socio?.NombreEmpresa ?? "")}}}</strong><br>
                      {{{H(f.Socio?.EmailContacto ?? "")}}}<br>
                      {{{H(f.Socio?.Direccion ?? "")}}}
                    </div>
                    <div class="box">
                      <div class="label">Fechas y pago</div>
                      Emision: <strong>{{{Date(f.FechaEmision)}}}</strong><br>
                      Vencimiento: <strong>{{{Date(f.FechaVencimiento)}}}</strong><br>
                      Condicion: <strong>{{{H(f.CondicionOperacion)}}}</strong><br>
                      Forma de pago: <strong>{{{H(f.FormaPago)}}}</strong>
                    </div>
                  </div>

                  <div class="box">
                    <div class="label">Datos de referencia DTE</div>
                    <div class="fiscal">
                      <div>Codigo de generacion:<br><span class="mono">{{{H(f.CodigoGeneracion)}}}</span></div>
                      <div>Numero de control:<br><span class="mono">{{{H(f.NumeroControl)}}}</span></div>
                      <div>Sello de recepcion:<br><span class="mono">{{{H(f.SelloRecepcion)}}}</span></div>
                      <div>Ambiente:<br><strong>{{{H(f.Ambiente)}}}</strong></div>
                    </div>
                    <p class="small muted">Consulta oficial: {{{H(verificacion)}}}</p>
                  </div>

                  {{{avisoFiscal}}}

                  <table>
                    <thead><tr><th>Concepto</th><th>Periodo</th><th class="right">Subtotal</th></tr></thead>
                    <tbody>
                      <tr><td>{{{H(f.Descripcion)}}}<br><span class="muted">{{{H(f.PlanNombre)}}}</span></td><td>{{{H(f.PlanPeriodo)}}}</td><td class="right">{{{Money(f.Subtotal)}}}</td></tr>
                      <tr><td colspan="2" class="right">IVA 13%</td><td class="right">{{{Money(f.Iva)}}}</td></tr>
                      <tr><td colspan="2" class="right total">Total</td><td class="right total">{{{Money(f.Total)}}}</td></tr>
                    </tbody>
                  </table>

                  <div class="box"><div class="label">Notas</div>{{{H(f.Notas)}}}</div>
                </div>
              </div>
            </body>
            </html>
            """;
    }

    private static readonly PlanMembresiaDto[] Planes =
    [
        new("Socios Fundadores", "$1,300", "anual", 1300m, "Membresia anual CASATIC - Socios Fundadores"),
        new("Socios Miembros", "$400 - $1,200", "anual", 400m, "Membresia anual CASATIC - Socios Miembros"),
        new("Socios Invitados", "$25 - $100", "trimestral", 25m, "Membresia trimestral CASATIC - Socios Invitados")
    ];

    private static readonly PlanMembresiaDto DefaultPlan = Planes[1];
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace CasaticDirectorio.Api.Controllers;

/// <summary>
/// Subida de archivos de imagen.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Socio")]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public UploadController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Sube una imagen y retorna su URL publica.
    /// Max. 10 MB. Acepta cualquier MIME image/*.
    /// </summary>
    [HttpPost("image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No se recibio ningun archivo" });

        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
        if (!contentType.StartsWith("image/"))
            return BadRequest(new { message = "Solo se permiten archivos de imagen" });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "El archivo no puede superar 10 MB" });

        var uploadsPath = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsPath);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".img";

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsPath, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return Ok(new { url = $"/uploads/{fileName}" });
    }

    /// <summary>
    /// Compatibilidad con pantallas existentes que suben logos.
    /// </summary>
    [HttpPost("logo")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public Task<IActionResult> UploadLogo(IFormFile file) => UploadImage(file);
}

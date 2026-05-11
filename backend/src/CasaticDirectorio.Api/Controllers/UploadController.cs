using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CasaticDirectorio.Api.Controllers;

/// <summary>
/// Subida de archivos (logos de empresas).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UploadController : ControllerBase
{
    private static readonly string[] _allowedTypes =
        ["image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"];

    /// <summary>
    /// Sube una imagen de logo y retorna su URL pública.
    /// Máx. 5 MB. Formatos: JPG, PNG, GIF, WebP.
    /// </summary>
    [HttpPost("logo")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No se recibió ningún archivo" });

        if (!_allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { message = "Solo se permiten imágenes JPG, PNG, GIF o WebP" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "El archivo no puede superar 5 MB" });

        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logos");
        Directory.CreateDirectory(uploadsPath);

        var extension = Path.GetExtension(file.FileName).ToLower();
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsPath, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return Ok(new { url = $"/logos/{fileName}" });
    }
}

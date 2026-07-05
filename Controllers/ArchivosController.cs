using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Linq;

namespace LaboratorioPUCE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArchivosController : ControllerBase
    {
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
        private readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        [HttpPost("upload/{tipo}")]
        public async Task<IActionResult> UploadImage(string tipo, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { mensaje = "No se proporcionó ningún archivo." });

            if (file.Length > MaxFileSize)
                return BadRequest(new { mensaje = "El archivo excede el tamaño máximo permitido de 5 MB." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                return BadRequest(new { mensaje = "Solo se permiten imágenes (jpg, png, gif, webp)." });

            if (tipo != "items" && tipo != "evidencias")
                return BadRequest(new { mensaje = "Tipo de archivo no válido." });

            var dateFolder = $"{DateTime.UtcNow.Year}/{DateTime.UtcNow.Month:D2}";
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", tipo, dateFolder);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(folderPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative URL
            var publicUrl = $"/uploads/{tipo}/{dateFolder}/{uniqueFileName}";

            return Ok(new { url = publicUrl, mensaje = "Archivo subido correctamente." });
        }
    }
}

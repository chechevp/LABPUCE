using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaboratorioPUCE.Data;
using LaboratorioPUCE.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LaboratorioPUCE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriasController : ControllerBase
    {
        private readonly LaboratorioContext _context;

        public CategoriasController(LaboratorioContext context)
        {
            _context = context;
        }

        private async Task<bool> IsAdmin()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ")) return false;
            
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _context.SesionesUsuario
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(s => s.Token == token && s.Activa == 1 && s.FechaExpira > DateTime.UtcNow);
                
            return session?.Usuario?.RolId == 1;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categorias = await _context.CategoriasItem
                .Where(c => c.Activo == 1)
                .OrderBy(c => c.Nombre)
                .ToListAsync();
            return Ok(categorias);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoriaItem model)
        {
            if (!await IsAdmin()) return Unauthorized(new { mensaje = "Acceso denegado." });
            if (string.IsNullOrWhiteSpace(model.Nombre)) return BadRequest(new { mensaje = "El nombre es obligatorio." });

            model.Nombre = model.Nombre.Trim();
            model.Activo = 1;

            _context.CategoriasItem.Add(model);
            await _context.SaveChangesAsync();

            return Ok(model);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoriaItem model)
        {
            if (!await IsAdmin()) return Unauthorized(new { mensaje = "Acceso denegado." });
            if (string.IsNullOrWhiteSpace(model.Nombre)) return BadRequest(new { mensaje = "El nombre es obligatorio." });

            var existing = await _context.CategoriasItem.FirstOrDefaultAsync(c => c.CategoriaId == id && c.Activo == 1);
            if (existing == null) return NotFound(new { mensaje = "Categoría no encontrada." });

            existing.Nombre = model.Nombre.Trim();
            existing.StockMinimo = model.StockMinimo;

            _context.CategoriasItem.Update(existing);
            await _context.SaveChangesAsync();

            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await IsAdmin()) return Unauthorized(new { mensaje = "Acceso denegado." });

            var existing = await _context.CategoriasItem.FirstOrDefaultAsync(c => c.CategoriaId == id && c.Activo == 1);
            if (existing == null) return NotFound(new { mensaje = "Categoría no encontrada." });

            // Logical delete
            existing.Activo = 0;
            _context.CategoriasItem.Update(existing);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Categoría eliminada correctamente." });
        }
    }
}

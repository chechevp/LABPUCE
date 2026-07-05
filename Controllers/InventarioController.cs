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
    public class InventarioController : ControllerBase
    {
        private readonly LaboratorioContext _context;

        public InventarioController(LaboratorioContext context)
        {
            _context = context;
        }

        // Helper to validate Admin session
        private async Task<Usuario?> ValidateAdminSessionAsync()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return null;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _context.SesionesUsuario
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(s => s.Token == token && s.Activa == 1 && s.FechaExpira > DateTime.UtcNow);

            if (session == null || session.Usuario == null || session.Usuario.RolId != 1) // 1 = Administrador
            {
                return null;
            }

            return session.Usuario;
        }

        // GET: api/inventario
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Both admins and students can list items, but students will only see active ones
            var items = await _context.ItemsInventario
                .AsNoTracking()
                .Include(i => i.Espacio)
                .ThenInclude(e => e!.Taller)
                .Include(i => i.Categoria)
                .Where(i => i.Activo == 1)
                .OrderByDescending(i => i.FechaCreacion)
                .ToListAsync();

            return Ok(items);
        }

        // GET: api/inventario/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.ItemsInventario
                .AsNoTracking()
                .Include(i => i.Espacio)
                .ThenInclude(e => e!.Taller)
                .Include(i => i.Categoria)
                .FirstOrDefaultAsync(i => i.ItemId == id && i.Activo == 1);

            if (item == null)
            {
                return NotFound(new { mensaje = "Elemento de inventario no encontrado." });
            }

            return Ok(item);
        }

        // POST: api/inventario
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ItemInventario model)
        {
            var adminUser = await ValidateAdminSessionAsync();
            if (adminUser == null)
            {
                return Unauthorized(new { mensaje = "Acceso denegado. Se requieren credenciales de Administrador." });
            }

            if (string.IsNullOrWhiteSpace(model.CodigoActivo) || string.IsNullOrWhiteSpace(model.Nombre))
            {
                return BadRequest(new { mensaje = "El código y el nombre del elemento son obligatorios." });
            }

            // Check code uniqueness
            var codeExists = await _context.ItemsInventario
                .AnyAsync(i => i.CodigoActivo.ToLower() == model.CodigoActivo.Trim().ToLower() && i.Activo == 1);

            if (codeExists)
            {
                return Conflict(new { mensaje = $"La codificación alfanumérica '{model.CodigoActivo}' ya se encuentra registrada en el inventario." });
            }

            // Ensure categories and spaces exist or fall back to seeded ones
            if (model.CategoriaId == 0) model.CategoriaId = 1;
            if (model.EspacioId == 0) model.EspacioId = 1;

            model.CodigoActivo = model.CodigoActivo.Trim();
            model.Nombre = model.Nombre.Trim();
            model.NumeroSerie = model.NumeroSerie?.Trim();
            if (model.Stock <= 0) model.Stock = 1;
            if (model.StockMinimo < 0) model.StockMinimo = 1;

            model.FechaCreacion = DateTime.UtcNow;
            model.Activo = 1;
            model.EstadoOperativo = "OPERATIVO";
            model.EstadoPrestamo = "DISPONIBLE";
            // Default 1 if not provided explicitly as 0 (for creation we assume model comes populated from JS)
            model.EsPublico = model.EsPublico == 0 ? 0 : 1;

            _context.ItemsInventario.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.ItemId }, model);
        }

        // PUT: api/inventario/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ItemInventario model)
        {
            var adminUser = await ValidateAdminSessionAsync();
            if (adminUser == null)
            {
                return Unauthorized(new { mensaje = "Acceso denegado. Se requieren credenciales de Administrador." });
            }

            var existingItem = await _context.ItemsInventario.FirstOrDefaultAsync(i => i.ItemId == id && i.Activo == 1);
            if (existingItem == null)
            {
                return NotFound(new { mensaje = "Elemento de inventario no encontrado." });
            }

            if (string.IsNullOrWhiteSpace(model.Nombre))
            {
                return BadRequest(new { mensaje = "El nombre del elemento es obligatorio." });
            }

            // Validate code uniqueness if code is changed
            if (existingItem.CodigoActivo.ToLower() != model.CodigoActivo.Trim().ToLower())
            {
                var codeExists = await _context.ItemsInventario
                    .AnyAsync(i => i.CodigoActivo.ToLower() == model.CodigoActivo.Trim().ToLower() && i.ItemId != id && i.Activo == 1);

                if (codeExists)
                {
                    return Conflict(new { mensaje = $"La codificación alfanumérica '{model.CodigoActivo}' ya se encuentra registrada en otro elemento." });
                }
                existingItem.CodigoActivo = model.CodigoActivo.Trim();
            }

            existingItem.Nombre = model.Nombre.Trim();
            existingItem.Descripcion = model.Descripcion;
            existingItem.Marca = model.Marca;
            existingItem.Modelo = model.Modelo;
            existingItem.NumeroSerie = model.NumeroSerie;
            existingItem.EsMaquinaria = model.EsMaquinaria;
            existingItem.EstadoOperativo = model.EstadoOperativo;
            existingItem.EstadoPrestamo = model.EstadoPrestamo;
            existingItem.FechaAdquisicion = model.FechaAdquisicion;
            existingItem.Observaciones = model.Observaciones;
            existingItem.Stock = model.Stock;
            existingItem.StockMinimo = model.StockMinimo;
            existingItem.EsPublico = model.EsPublico;
            existingItem.ImagenUrl = model.ImagenUrl;
            if (model.EspacioId > 0) existingItem.EspacioId = model.EspacioId;
            if (model.CategoriaId > 0) existingItem.CategoriaId = model.CategoriaId;

            _context.ItemsInventario.Update(existingItem);
            await _context.SaveChangesAsync();

            return Ok(existingItem);
        }

        // DELETE: api/inventario/{id} (Soft Delete as required by inventory records)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var adminUser = await ValidateAdminSessionAsync();
            if (adminUser == null)
            {
                return Unauthorized(new { mensaje = "Acceso denegado. Se requieren credenciales de Administrador." });
            }

            var item = await _context.ItemsInventario.FirstOrDefaultAsync(i => i.ItemId == id && i.Activo == 1);
            if (item == null)
            {
                return NotFound(new { mensaje = "Elemento de inventario no encontrado." });
            }

            // Soft delete
            item.Activo = 0;
            _context.ItemsInventario.Update(item);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Elemento de inventario eliminado correctamente." });
        }

        public class AgregarStockRequest
        {
            public int Cantidad { get; set; }
        }

        [HttpPost("{id}/agregar-stock")]
        public async Task<IActionResult> AgregarStock(int id, [FromBody] AgregarStockRequest req)
        {
            var adminUser = await ValidateAdminSessionAsync();
            if (adminUser == null)
            {
                return Unauthorized(new { mensaje = "Acceso denegado. Se requieren credenciales de Administrador." });
            }

            if (req.Cantidad <= 0)
            {
                return BadRequest(new { mensaje = "La cantidad a agregar debe ser mayor a 0." });
            }

            var item = await _context.ItemsInventario.FirstOrDefaultAsync(i => i.ItemId == id && i.Activo == 1);
            if (item == null)
            {
                return NotFound(new { mensaje = "Elemento de inventario no encontrado." });
            }

            item.Stock += req.Cantidad;
            _context.ItemsInventario.Update(item);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = $"Stock actualizado exitosamente. Nuevo stock: {item.Stock}", nuevoStock = item.Stock });
        }

        [HttpPost("{id}/reparar-defectuoso")]
        public async Task<IActionResult> RepararDefectuoso(int id, [FromBody] AgregarStockRequest req)
        {
            var adminUser = await ValidateAdminSessionAsync();
            if (adminUser == null)
                return Unauthorized(new { mensaje = "Acceso denegado." });

            if (req.Cantidad <= 0)
                return BadRequest(new { mensaje = "La cantidad a reparar debe ser mayor a 0." });

            var item = await _context.ItemsInventario.FirstOrDefaultAsync(i => i.ItemId == id && i.Activo == 1);
            if (item == null)
                return NotFound(new { mensaje = "Elemento no encontrado." });

            if (req.Cantidad > item.StockDefectuoso)
                return BadRequest(new { mensaje = $"No puedes reparar más elementos de los defectuosos registrados ({item.StockDefectuoso})." });

            item.StockDefectuoso -= req.Cantidad;
            item.Stock += req.Cantidad;
            
            _context.ItemsInventario.Update(item);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = $"Se han reparado {req.Cantidad} elementos. Nuevo stock: {item.Stock}" });
        }

        // GET: api/inventario/metadata (for dropdown lists)
        [HttpGet("metadata")]
        public async Task<IActionResult> GetMetadata()
        {
            var categories = await _context.CategoriasItem.AsNoTracking().ToListAsync();
            var spaces = await _context.Espacios.Include(e => e.Taller).AsNoTracking().ToListAsync();
            return Ok(new { categories, spaces });
        }
    }
}

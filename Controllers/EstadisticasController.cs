using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaboratorioPUCE.Data;

namespace LaboratorioPUCE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstadisticasController : ControllerBase
    {
        private readonly LaboratorioContext _context;

        public EstadisticasController(LaboratorioContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerEstadisticas()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { mensaje = "Token ausente." });

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _context.SesionesUsuario
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(s => s.Token == token && s.Activa == 1 && s.FechaExpira > DateTime.UtcNow);

            if (session == null || session.Usuario == null)
                return Unauthorized(new { mensaje = "Sesión inválida o expirada." });

            bool isAdmin = session.Usuario.RolId == 1 || session.Usuario.RolId == 3;

            var prestamosQuery = _context.Prestamos
                .Include(p => p.Usuario)
                .Include(p => p.Item)
                .ThenInclude(i => i!.Categoria)
                .Where(p => p.Estado == "DEVUELTO");

            if (!isAdmin)
            {
                prestamosQuery = prestamosQuery.Where(p => p.UsuarioId == session.UsuarioId);
            }

            var prestamos = await prestamosQuery.ToListAsync();

            // Aggregations
            var porElemento = prestamos
                .GroupBy(p => p.Item != null ? p.Item.Nombre : "Desconocido")
                .Select(g => new { Nombre = g.Key, Cantidad = g.Sum(p => p.Cantidad) })
                .OrderByDescending(x => x.Cantidad)
                .ToList();

            var porCategoria = prestamos
                .GroupBy(p => p.Item != null && p.Item.Categoria != null ? p.Item.Categoria.Nombre : "Sin Categoría")
                .Select(g => new { Nombre = g.Key, Cantidad = g.Sum(p => p.Cantidad) })
                .OrderByDescending(x => x.Cantidad)
                .ToList();

            var porFecha = prestamos
                .Where(p => p.FechaDevolucion.HasValue)
                .GroupBy(p => p.FechaDevolucion!.Value.Date.ToString("yyyy-MM-dd"))
                .Select(g => new { Fecha = g.Key, Cantidad = g.Sum(p => p.Cantidad) })
                .OrderBy(x => x.Fecha)
                .ToList();

            object result;
            if (isAdmin)
            {
                var porEstudiante = prestamos
                    .GroupBy(p => p.Usuario != null ? $"{p.Usuario.Nombre} {p.Usuario.Apellido}" : "Desconocido")
                    .Select(g => new { Nombre = g.Key, Cantidad = g.Sum(p => p.Cantidad) })
                    .OrderByDescending(x => x.Cantidad)
                    .ToList();

                result = new
                {
                    PorElemento = porElemento,
                    PorCategoria = porCategoria,
                    PorFecha = porFecha,
                    PorEstudiante = porEstudiante
                };
            }
            else
            {
                result = new
                {
                    PorElemento = porElemento,
                    PorCategoria = porCategoria,
                    PorFecha = porFecha
                };
            }

            return Ok(result);
        }
    }
}

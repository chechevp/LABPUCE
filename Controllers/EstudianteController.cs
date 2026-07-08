using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LaboratorioPUCE.Core.Interfaces;
using System.Linq;
using LaboratorioPUCE.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace LaboratorioPUCE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstudianteController : ControllerBase
    {
        private readonly IConsultasService _consultasService;
        private readonly IPrestamosService _prestamosService;
        private readonly LaboratorioContext _context;

        public EstudianteController(IConsultasService consultasService, IPrestamosService prestamosService, LaboratorioContext context)
        {
            _consultasService = consultasService;
            _prestamosService = prestamosService;
            _context = context;
        }

        [HttpGet("catalogo")]
        public async Task<IActionResult> ObtenerCatalogo([FromQuery] string q = "")
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { mensaje = "Token ausente." });
            }

            // Simple token validation (in a real app, use Authorize attribute and Identity/JWT)
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _context.SesionesUsuario.FirstOrDefaultAsync(s => s.Token == token && s.Activa == 1 && s.FechaExpira > DateTime.UtcNow);
            if (session == null)
            {
                return Unauthorized(new { mensaje = "Sesión inválida o expirada." });
            }

            var catalogo = await _consultasService.ObtenerCatalogoDisponibleAsync(q);
            return Ok(catalogo);
        }

        public class SolicitudPrestamoRequest
        {
            public string NombreItem { get; set; } = string.Empty;
            public int Cantidad { get; set; }
            public DateTime? FechaDevolucion { get; set; }
        }

        [HttpPost("prestamo")]
        public async Task<IActionResult> SolicitarPrestamo([FromBody] SolicitudPrestamoRequest request)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { mensaje = "Token ausente." });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _context.SesionesUsuario.FirstOrDefaultAsync(s => s.Token == token && s.Activa == 1 && s.FechaExpira > DateTime.UtcNow);
            if (session == null)
            {
                return Unauthorized(new { mensaje = "Sesión inválida o expirada." });
            }

            if (string.IsNullOrWhiteSpace(request.NombreItem) || request.Cantidad <= 0)
            {
                return BadRequest(new { mensaje = "Datos de solicitud inválidos." });
            }

            var tieneVencidos = await _context.Prestamos
                .AnyAsync(p => p.UsuarioId == session.UsuarioId && p.Estado == "APROBADO" && p.FechaDevolucion < DateTime.UtcNow);

            if (tieneVencidos)
            {
                return BadRequest(new { mensaje = "No puedes solicitar más préstamos. Tienes ítems con fecha de devolución vencida." });
            }

            var exito = await _prestamosService.SolicitarPrestamoAsync(session.UsuarioId, request.NombreItem, request.Cantidad, request.FechaDevolucion);
            if (!exito)
            {
                return BadRequest(new { mensaje = "No se pudo procesar la solicitud. Verifica el stock." });
            }

            return Ok(new { mensaje = "Solicitud de préstamo enviada exitosamente." });
        }

        public class SolicitudPrestamoItemRequest
        {
            public string NombreItem { get; set; } = string.Empty;
            public int Cantidad { get; set; }
        }

        public class SolicitudPrestamoBatchRequest
        {
            public System.Collections.Generic.List<SolicitudPrestamoItemRequest> Items { get; set; } = new System.Collections.Generic.List<SolicitudPrestamoItemRequest>();
            public DateTime? FechaDevolucion { get; set; }
        }

        [HttpPost("prestamo-batch")]
        public async Task<IActionResult> SolicitarPrestamoBatch([FromBody] SolicitudPrestamoBatchRequest request)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { mensaje = "Token ausente." });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _context.SesionesUsuario.FirstOrDefaultAsync(s => s.Token == token && s.Activa == 1 && s.FechaExpira > DateTime.UtcNow);
            if (session == null)
            {
                return Unauthorized(new { mensaje = "Sesión inválida o expirada." });
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new { mensaje = "El carrito está vacío." });
            }

            var tieneVencidos = await _context.Prestamos
                .AnyAsync(p => p.UsuarioId == session.UsuarioId && p.Estado == "APROBADO" && p.FechaDevolucion < DateTime.UtcNow);

            if (tieneVencidos)
            {
                return BadRequest(new { mensaje = "No puedes solicitar más préstamos. Tienes ítems con fecha de devolución vencida." });
            }

            var exito = await _prestamosService.SolicitarPrestamoBatchAsync(session.UsuarioId, request.Items.Select(i => (i.NombreItem, i.Cantidad)).ToList(), request.FechaDevolucion);
            
            if (!exito)
            {
                return BadRequest(new { mensaje = "No se pudo procesar toda la solicitud. Verifica el stock de los elementos." });
            }

            return Ok(new { mensaje = "Solicitud múltiple enviada exitosamente." });
        }

        [HttpGet("mis-solicitudes")]
        public async Task<IActionResult> MisSolicitudes()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { mensaje = "Token ausente." });

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _context.SesionesUsuario.FirstOrDefaultAsync(s => s.Token == token && s.Activa == 1 && s.FechaExpira > DateTime.UtcNow);
            if (session == null)
                return Unauthorized(new { mensaje = "Sesión inválida o expirada." });

            var misPrestamos = await _context.Prestamos
                .Include(p => p.Item)
                .Where(p => p.UsuarioId == session.UsuarioId)
                .OrderByDescending(p => p.FechaSolicitud)
                .Select(p => new
                {
                    p.CodigoReserva,
                    NombreItem = p.Item != null ? p.Item.Nombre : "Desconocido",
                    p.Cantidad,
                    p.Estado,
                    p.FechaSolicitud,
                    p.FechaDevolucion,
                    p.ComentarioAdmin,
                    p.EvidenciaUrl
                })
                .ToListAsync();

            return Ok(misPrestamos);
        }

        public class SolicitarDevolucionReq
        {
            public string? EvidenciaUrl { get; set; }
            public string? Comentario { get; set; }
        }

        [HttpPost("prestamo/{codigoReserva}/devolver")]
        public async Task<IActionResult> SolicitarDevolucion(string codigoReserva, [FromBody] SolicitarDevolucionReq req)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { mensaje = "Token ausente." });

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _context.SesionesUsuario.FirstOrDefaultAsync(s => s.Token == token && s.Activa == 1 && s.FechaExpira > DateTime.UtcNow);
            if (session == null)
                return Unauthorized(new { mensaje = "Sesión inválida o expirada." });

            var exito = await _prestamosService.SolicitarDevolucionAsync(codigoReserva, session.UsuarioId, req?.EvidenciaUrl, req?.Comentario);

            if (!exito)
                return BadRequest(new { mensaje = "El préstamo no fue encontrado o no está en estado APROBADO." });

            return Ok(new { mensaje = "Solicitud de devolución enviada al administrador exitosamente." });
        }
    }
}

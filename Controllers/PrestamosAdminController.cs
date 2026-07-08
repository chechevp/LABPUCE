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
    public class PrestamosAdminController : ControllerBase
    {
        private readonly IPrestamosService _prestamosService;
        private readonly LaboratorioContext _context;

        public PrestamosAdminController(IPrestamosService prestamosService, LaboratorioContext context)
        {
            _prestamosService = prestamosService;
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

        private async Task<bool> IsAdminOrDocente()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ")) return false;
            
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _context.SesionesUsuario
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(s => s.Token == token && s.Activa == 1 && s.FechaExpira > DateTime.UtcNow);
                
            return session?.Usuario?.RolId == 1 || session?.Usuario?.RolId == 3;
        }

        [HttpGet("pendientes")]
        public async Task<IActionResult> ObtenerPendientes()
        {
            if (!await IsAdminOrDocente()) return Unauthorized(new { mensaje = "Acceso denegado." });

            var solicitudes = await _prestamosService.ObtenerSolicitudesPendientesAsync();

            var dtos = solicitudes.Select(s => new
            {
                PrestamoId = s.PrestamoId,
                CodigoReserva = s.CodigoReserva,
                Usuario = $"{s.Usuario?.Nombre} {s.Usuario?.Apellido} ({s.Usuario?.Correo})",
                NombreItem = s.Item?.Nombre,
                ModeloItem = s.Item?.Modelo,
                Cantidad = s.Cantidad,
                Estado = s.Estado,
                FechaSolicitud = s.FechaSolicitud,
                FechaDevolucion = s.FechaDevolucion
            });

            return Ok(dtos);
        }

        [HttpGet("devoluciones")]
        public async Task<IActionResult> ObtenerSolicitudesDevolucion()
        {
            if (!await IsAdminOrDocente()) return Unauthorized(new { mensaje = "Acceso denegado." });

            var solicitudes = await _prestamosService.ObtenerSolicitudesDevolucionAsync();
            var dtos = solicitudes.Select(s => new
            {
                PrestamoId = s.PrestamoId,
                CodigoReserva = s.CodigoReserva,
                Usuario = $"{s.Usuario?.Nombre} {s.Usuario?.Apellido} ({s.Usuario?.Correo})",
                NombreItem = s.Item?.Nombre,
                ModeloItem = s.Item?.Modelo,
                Cantidad = s.Cantidad,
                Estado = s.Estado,
                FechaSolicitud = s.FechaSolicitud,
                FechaDevolucion = s.FechaDevolucion,
                EvidenciaUrl = s.EvidenciaUrl,
                ComentarioAdmin = s.ComentarioAdmin
            });

            return Ok(dtos);
        }

        public class EstadoRequest
        {
            public string Estado { get; set; } = string.Empty;
            public string? ComentarioAdmin { get; set; }
            public bool EsDefectuoso { get; set; }
        }

        [HttpPut("{codigoReserva}/estado")]
        public async Task<IActionResult> ActualizarEstado(string codigoReserva, [FromBody] EstadoRequest request)
        {
            if (!await IsAdminOrDocente()) return Unauthorized(new { mensaje = "Acceso denegado." });

            if (request.Estado != "APROBADO" && request.Estado != "RECHAZADO")
                return BadRequest(new { mensaje = "Estado no válido." });

            var exito = await _prestamosService.ActualizarEstadoSolicitudAsync(codigoReserva, request.Estado, request.ComentarioAdmin);
            
            if (!exito)
                return NotFound(new { mensaje = "Solicitud no encontrada." });

            return Ok(new { mensaje = $"Solicitud {request.Estado.ToLower()} correctamente." });
        }

        [HttpPut("item/{prestamoId}/estado")]
        public async Task<IActionResult> ActualizarEstadoPorId(int prestamoId, [FromBody] EstadoRequest request)
        {
            if (!await IsAdminOrDocente()) return Unauthorized(new { mensaje = "Acceso denegado." });

            if (request.Estado != "APROBADO" && request.Estado != "RECHAZADO")
                return BadRequest(new { mensaje = "Estado no válido." });

            var exito = await _prestamosService.ActualizarEstadoSolicitudPorIdAsync(prestamoId, request.Estado, request.ComentarioAdmin);
            
            if (!exito)
                return NotFound(new { mensaje = "Item de solicitud no encontrado." });

            return Ok(new { mensaje = $"Ítem {request.Estado.ToLower()} correctamente." });
        }

        [HttpGet("historial")]
        public async Task<IActionResult> ObtenerHistorial()
        {
            if (!await IsAdminOrDocente()) return Unauthorized(new { mensaje = "Acceso denegado." });

            var prestamos = await _prestamosService.ObtenerTodosPrestamosAsync();

            var agrupadas = prestamos
                .Select(s => new
                {
                    PrestamoId = s.PrestamoId,
                    CodigoReserva = s.CodigoReserva,
                    Usuario = $"{s.Usuario?.Nombre} {s.Usuario?.Apellido} ({s.Usuario?.Correo})",
                    NombreItem = s.Item?.Nombre,
                    ModeloItem = s.Item?.Modelo,
                    Cantidad = s.Cantidad,
                    FechaSolicitud = s.FechaSolicitud,
                    FechaDevolucion = s.FechaDevolucion,
                    Estado = s.Estado,
                    EvidenciaUrl = s.EvidenciaUrl,
                    ComentarioAdmin = s.ComentarioAdmin
                });

            return Ok(agrupadas);
        }

        [HttpGet("historial/{codigoReserva}")]
        public async Task<IActionResult> ObtenerDetalle(string codigoReserva)
        {
            if (!await IsAdminOrDocente()) return Unauthorized(new { mensaje = "Acceso denegado." });

            var prestamo = await _prestamosService.ObtenerPrestamoPorCodigoAsync(codigoReserva);
            if (prestamo == null) return NotFound(new { mensaje = "Préstamo no encontrado." });

            return Ok(new
            {
                CodigoReserva = prestamo.CodigoReserva,
                Usuario = $"{prestamo.Usuario?.Nombre} {prestamo.Usuario?.Apellido} ({prestamo.Usuario?.Correo})",
                NombreItem = prestamo.Item?.Nombre,
                ModeloItem = prestamo.Item?.Modelo,
                Cantidad = prestamo.Cantidad,
                Estado = prestamo.Estado,
                FechaSolicitud = prestamo.FechaSolicitud,
                FechaAprobacion = prestamo.FechaAprobacion,
                FechaDevolucion = prestamo.FechaDevolucion,
                ComentarioAdmin = prestamo.ComentarioAdmin
            });
        }

        public class DevolucionRequest
        {
            public string? ComentarioAdmin { get; set; }
            public int CantidadDefectuosa { get; set; }
            public string? EvidenciaUrl { get; set; }
        }

        [HttpPost("{codigoReserva}/devolver")]
        public async Task<IActionResult> MarcarDevuelto(string codigoReserva, [FromBody] DevolucionRequest req)
        {
            if (!await IsAdminOrDocente()) return Unauthorized(new { mensaje = "Acceso denegado." });

            var exito = await _prestamosService.MarcarComoDevueltoAsync(codigoReserva, req.ComentarioAdmin, req.CantidadDefectuosa);

            if (!exito)
                return BadRequest(new { mensaje = "Préstamo no encontrado o no está en estado APROBADO." });

            return Ok(new { mensaje = "Préstamo marcado como devuelto correctamente." });
        }

        [HttpPost("{codigoReserva}/devolver/rechazar")]
        public async Task<IActionResult> RechazarDevolucion(string codigoReserva, [FromBody] DevolucionRequest req)
        {
            if (!await IsAdminOrDocente()) return Unauthorized(new { mensaje = "Acceso denegado." });

            var exito = await _prestamosService.RechazarDevolucionAsync(codigoReserva, req.ComentarioAdmin);

            if (!exito)
                return BadRequest(new { mensaje = "No se pudo rechazar la devolución." });

            return Ok(new { mensaje = "Devolución rechazada. El ítem sigue prestado." });
        }

        [HttpPost("item/{prestamoId}/devolver")]
        public async Task<IActionResult> MarcarDevueltoPorId(int prestamoId, [FromBody] DevolucionRequest req)
        {
            if (!await IsAdminOrDocente()) return Unauthorized(new { mensaje = "Acceso denegado." });

            var exito = await _prestamosService.MarcarComoDevueltoPorIdAsync(prestamoId, req.ComentarioAdmin, req.CantidadDefectuosa, req.EvidenciaUrl);

            if (!exito)
                return BadRequest(new { mensaje = "Préstamo no encontrado o no está en estado APROBADO." });

            return Ok(new { mensaje = "Ítem marcado como devuelto correctamente." });
        }

        [HttpPost("item/{prestamoId}/devolver/rechazar")]
        public async Task<IActionResult> RechazarDevolucionPorId(int prestamoId, [FromBody] DevolucionRequest req)
        {
            if (!await IsAdminOrDocente()) return Unauthorized(new { mensaje = "Acceso denegado." });

            var exito = await _prestamosService.RechazarDevolucionPorIdAsync(prestamoId, req.ComentarioAdmin, req.EvidenciaUrl);

            if (!exito)
                return BadRequest(new { mensaje = "No se pudo rechazar la devolución." });

            return Ok(new { mensaje = "Devolución de ítem rechazada." });
        }
    }
}

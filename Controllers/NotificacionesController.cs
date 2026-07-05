using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaboratorioPUCE.Core.Interfaces;
using LaboratorioPUCE.Data;

namespace LaboratorioPUCE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificacionesController : ControllerBase
    {
        private readonly INotificacionesService _notificacionesService;
        private readonly LaboratorioContext _context;

        public NotificacionesController(INotificacionesService notificacionesService, LaboratorioContext context)
        {
            _notificacionesService = notificacionesService;
            _context = context;
        }

        // Helper para obtener usuario
        private async Task<int?> GetCurrentUserIdAsync()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            var token = authHeader.Substring("Bearer ".Length).Trim();

            var session = await _context.SesionesUsuario
                .FirstOrDefaultAsync(s => s.Token == token && s.Activa == 1 && s.FechaExpira > DateTime.UtcNow);
            
            return session?.UsuarioId;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerNotificaciones()
        {
            var usuarioId = await GetCurrentUserIdAsync();
            if (usuarioId == null) return Unauthorized(new { message = "Sesión inválida." });

            var notificaciones = await _notificacionesService.ObtenerNotificacionesAsync(usuarioId.Value);
            var noLeidas = await _notificacionesService.ObtenerNotificacionesNoLeidasCountAsync(usuarioId.Value);

            return Ok(new
            {
                notificaciones = notificaciones.Select(n => new
                {
                    n.NotificacionId,
                    n.Titulo,
                    n.Mensaje,
                    n.ReferenciaId,
                    n.Tipo,
                    n.Leida,
                    Fecha = n.FechaCreacion.ToString("O")
                }),
                noLeidas
            });
        }

        [HttpPost("{id}/leer")]
        public async Task<IActionResult> MarcarComoLeida(int id)
        {
            var usuarioId = await GetCurrentUserIdAsync();
            if (usuarioId == null) return Unauthorized();

            var success = await _notificacionesService.MarcarComoLeidaAsync(id, usuarioId.Value);
            if (!success) return NotFound();

            return Ok(new { success = true });
        }

        [HttpPost("leer-todas")]
        public async Task<IActionResult> MarcarTodasComoLeidas()
        {
            var usuarioId = await GetCurrentUserIdAsync();
            if (usuarioId == null) return Unauthorized();

            await _notificacionesService.MarcarTodasComoLeidasAsync(usuarioId.Value);

            return Ok(new { success = true });
        }
    }
}

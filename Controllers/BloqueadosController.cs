using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaboratorioPUCE.Data;
using LaboratorioPUCE.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using LaboratorioPUCE.Core.Entities;

namespace LaboratorioPUCE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BloqueadosController : ControllerBase
    {
        private readonly LaboratorioContext _context;

        public BloqueadosController(LaboratorioContext context)
        {
            _context = context;
        }

        private async Task<Usuario?> GetAuthenticatedUserAsync()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer ")) return null;

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var sesion = await _context.SesionesUsuario
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(s => s.Token == token && s.Activa == 1 && s.FechaExpira > DateTime.UtcNow);

            return sesion?.Usuario;
        }

        [HttpGet]
        public async Task<IActionResult> GetBloqueados()
        {
            var usuarioActual = await GetAuthenticatedUserAsync();
            if (usuarioActual == null || (usuarioActual.RolId != 1 && usuarioActual.RolId != 3))
            {
                return Unauthorized(new { message = "No autorizado" });
            }

            var now = DateTime.UtcNow;

            var usuariosBloqueados = await _context.Usuarios
                .Where(u => u.RolId == 2) // Solo estudiantes
                .Select(u => new
                {
                    UsuarioId = u.UsuarioId,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Correo = u.Correo,
                    PrestamosVencidos = _context.Prestamos
                        .Where(p => p.UsuarioId == u.UsuarioId && p.Estado == "APROBADO" && p.FechaDevolucion.HasValue && p.FechaDevolucion.Value < now)
                        .Select(p => new
                        {
                            p.CodigoReserva,
                            p.FechaDevolucion,
                            p.Cantidad
                        }).ToList()
                })
                .Where(u => u.PrestamosVencidos.Any())
                .ToListAsync();

            return Ok(usuariosBloqueados);
        }

        [HttpPost("notificar/{usuarioId}")]
        public async Task<IActionResult> NotificarEstudiante(int usuarioId)
        {
            var usuarioActual = await GetAuthenticatedUserAsync();
            if (usuarioActual == null || (usuarioActual.RolId != 1 && usuarioActual.RolId != 3))
            {
                return Unauthorized(new { message = "No autorizado" });
            }

            var estudiante = await _context.Usuarios.FindAsync(usuarioId);
            if (estudiante == null || estudiante.RolId != 2)
            {
                return NotFound(new { message = "Estudiante no encontrado" });
            }

            var notificacion = new LaboratorioPUCE.Core.Entities.Notificacion
            {
                UsuarioId = usuarioId,
                Titulo = "¡Devolución Atrasada!",
                Mensaje = "Tienes préstamos de equipos que han vencido. Por favor, acércate al laboratorio para realizar la devolución lo más pronto posible.",
                Tipo = "ALERTA",
                Leida = 0,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificación enviada exitosamente" });
        }
    }
}

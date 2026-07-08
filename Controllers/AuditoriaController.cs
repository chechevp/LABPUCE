using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaboratorioPUCE.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LaboratorioPUCE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditoriaController : ControllerBase
    {
        private readonly LaboratorioContext _context;

        public AuditoriaController(LaboratorioContext context)
        {
            _context = context;
        }

        private async Task<LaboratorioPUCE.Models.Usuario?> ValidateAdminSessionAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
                return null;

            var token = authHeader.ToString().Replace("Bearer ", "");
            var session = await _context.SesionesUsuario
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(s => s.Token == token && s.Activa == 1 && s.FechaExpira > System.DateTime.UtcNow);

            if (session == null || session.Usuario == null || session.Usuario.RolId != 1)
                return null;

            return session.Usuario;
        }

        // GET: api/auditoria
        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            var adminUser = await ValidateAdminSessionAsync();
            if (adminUser == null)
            {
                return Unauthorized(new { mensaje = "Acceso denegado. Se requieren credenciales de Administrador." });
            }

            var logs = await _context.LogsTransacciones
                .Include(l => l.Usuario)
                .OrderByDescending(l => l.FechaHora)
                .Select(l => new
                {
                    l.LogId,
                    Usuario = $"{l.Usuario.Nombre} {l.Usuario.Apellido}",
                    l.Accion,
                    l.Detalle,
                    l.ReferenciaId,
                    l.FechaHora
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}

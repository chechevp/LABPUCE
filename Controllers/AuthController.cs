using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaboratorioPUCE.Data;
using LaboratorioPUCE.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LaboratorioPUCE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LaboratorioContext _context;

        public AuthController(LaboratorioContext context)
        {
            _context = context;
        }

        public class LoginRequest
        {
            public string Correo { get; set; } = string.Empty;
            public string Contrasena { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Contrasena))
            {
                return BadRequest(new { mensaje = "El correo y la contraseña son requeridos." });
            }

            // 1. Validate email domain (@pucesa.edu.ec)
            var normalizedEmail = request.Correo.Trim().ToLower();
            if (!normalizedEmail.EndsWith("@pucesa.edu.ec"))
            {
                return Unauthorized(new { mensaje = "El correo institucional debe pertenecer al dominio @pucesa.edu.ec." });
            }

            // 2. Fetch active user including Rol
            var user = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Correo.ToLower() == normalizedEmail && u.Activo == 1);

            if (user == null)
            {
                return Unauthorized(new { mensaje = "Credenciales incorrectas o usuario inactivo." });
            }

            // 3. Verify password
            var inputHash = LaboratorioContext.HashPassword(request.Contrasena);
            if (user.PasswordHash != inputHash)
            {
                return Unauthorized(new { mensaje = "Credenciales incorrectas o usuario inactivo." });
            }

            // Update last access
            user.UltimoAcceso = DateTime.UtcNow;
            _context.Usuarios.Update(user);

            // 4. Create database-backed Session Token
            var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var session = new SesionUsuario
            {
                SesionId = Guid.NewGuid().ToString(),
                UsuarioId = user.UsuarioId,
                Token = rawToken,
                FechaInicio = DateTime.UtcNow,
                FechaExpira = DateTime.UtcNow.AddHours(4),
                IpOrigen = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Activa = 1
            };

            _context.SesionesUsuario.Add(session);
            await _context.SaveChangesAsync();

            // Determine redirect target based on Rol
            // RolId 1 is Administrador, RolId 2 is Estudiante
            string redirectUrl = user.RolId == 1 ? "/dashboard.html?view=admin" : "/dashboard.html?view=student";

            return Ok(new
            {
                token = rawToken,
                usuarioId = user.UsuarioId,
                nombre = $"{user.Nombre} {user.Apellido}",
                correo = user.Correo,
                rol = user.Rol?.Nombre ?? "Estudiante",
                rolId = user.RolId,
                redirect = redirectUrl
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return BadRequest(new { mensaje = "Sesión no válida o token ausente." });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _context.SesionesUsuario.FirstOrDefaultAsync(s => s.Token == token && s.Activa == 1);

            if (session != null)
            {
                session.Activa = 0;
                _context.SesionesUsuario.Update(session);
                await _context.SaveChangesAsync();
            }

            return Ok(new { mensaje = "Sesión cerrada correctamente." });
        }

        [HttpGet("validate")]
        public async Task<IActionResult> ValidateSession()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { mensaje = "Token ausente." });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _context.SesionesUsuario
                .AsNoTracking()
                .Include(s => s.Usuario)
                .ThenInclude(u => u!.Rol)
                .FirstOrDefaultAsync(s => s.Token == token && s.Activa == 1 && s.FechaExpira > DateTime.UtcNow);

            if (session == null || session.Usuario == null)
            {
                return Unauthorized(new { mensaje = "Sesión inválida o expirada." });
            }

            return Ok(new
            {
                usuarioId = session.Usuario.UsuarioId,
                nombre = $"{session.Usuario.Nombre} {session.Usuario.Apellido}",
                correo = session.Usuario.Correo,
                rol = session.Usuario.Rol?.Nombre ?? "Estudiante",
                rolId = session.Usuario.RolId
            });
        }
    }
}

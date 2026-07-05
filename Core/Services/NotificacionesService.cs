using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LaboratorioPUCE.Core.Entities;
using LaboratorioPUCE.Core.Interfaces;
using LaboratorioPUCE.Data;

namespace LaboratorioPUCE.Core.Services
{
    public class NotificacionesService : INotificacionesService
    {
        private readonly LaboratorioContext _context;

        public NotificacionesService(LaboratorioContext context)
        {
            _context = context;
        }

        public async Task<bool> CrearNotificacionAsync(int usuarioId, string titulo, string mensaje, string? referenciaId = null, string? tipo = null)
        {
            var notificacion = new Notificacion
            {
                UsuarioId = usuarioId,
                Titulo = titulo,
                Mensaje = mensaje,
                ReferenciaId = referenciaId,
                Tipo = tipo ?? "INFO",
                FechaCreacion = DateTime.UtcNow
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CrearNotificacionParaAdminsAsync(string titulo, string mensaje, string? referenciaId = null, string? tipo = null)
        {
            // Obtener todos los administradores (NivelAcceso == 10)
            var admins = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Rol != null && u.Rol.NivelAcceso >= 10 && u.Activo == 1)
                .ToListAsync();

            if (!admins.Any())
                return false;

            var notificaciones = admins.Select(admin => new Notificacion
            {
                UsuarioId = admin.UsuarioId,
                Titulo = titulo,
                Mensaje = mensaje,
                ReferenciaId = referenciaId,
                Tipo = tipo ?? "INFO",
                FechaCreacion = DateTime.UtcNow
            }).ToList();

            _context.Notificaciones.AddRange(notificaciones);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Notificacion>> ObtenerNotificacionesAsync(int usuarioId)
        {
            return await _context.Notificaciones
                .Where(n => n.UsuarioId == usuarioId)
                .OrderByDescending(n => n.FechaCreacion)
                .Take(50) // Limitar a las últimas 50
                .ToListAsync();
        }

        public async Task<int> ObtenerNotificacionesNoLeidasCountAsync(int usuarioId)
        {
            return await _context.Notificaciones
                .CountAsync(n => n.UsuarioId == usuarioId && n.Leida == 0);
        }

        public async Task<bool> MarcarComoLeidaAsync(int notificacionId, int usuarioId)
        {
            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.NotificacionId == notificacionId && n.UsuarioId == usuarioId);

            if (notificacion == null)
                return false;

            notificacion.Leida = 1;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarcarTodasComoLeidasAsync(int usuarioId)
        {
            var notificaciones = await _context.Notificaciones
                .Where(n => n.UsuarioId == usuarioId && n.Leida == 0)
                .ToListAsync();

            if (!notificaciones.Any())
                return true;

            foreach (var n in notificaciones)
            {
                n.Leida = 1;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

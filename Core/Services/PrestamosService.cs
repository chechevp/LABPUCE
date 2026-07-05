using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LaboratorioPUCE.Core.Interfaces;
using LaboratorioPUCE.Core.Entities;
using LaboratorioPUCE.Data;

namespace LaboratorioPUCE.Core.Services
{
    public class PrestamosService : IPrestamosService
    {
        private readonly LaboratorioContext _context;
        private readonly INotificacionesService _notificacionesService;

        public PrestamosService(LaboratorioContext context, INotificacionesService notificacionesService)
        {
            _context = context;
            _notificacionesService = notificacionesService;
        }

        public async Task<bool> SolicitarPrestamoAsync(int usuarioId, string nombreItem, int cantidad, DateTime? fechaDevolucion = null)
        {
            if (cantidad <= 0) return false;

            // Encontrar un ítem que tenga suficiente stock
            var item = await _context.ItemsInventario
                .FirstOrDefaultAsync(i => i.Nombre == nombreItem && i.Activo == 1 && i.EstadoOperativo == "OPERATIVO" && i.Stock >= cantidad);

            if (item == null)
            {
                // No hay suficiente stock
                return false;
            }

            string codigoReserva = "RES-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + new Random().Next(100, 999);

            // Restar el stock
            item.Stock -= cantidad;

            var prestamo = new Prestamo
            {
                UsuarioId = usuarioId,
                ItemId = item.ItemId,
                Cantidad = cantidad,
                Estado = "PENDIENTE",
                CodigoReserva = codigoReserva,
                FechaSolicitud = DateTime.UtcNow,
                FechaDevolucion = fechaDevolucion
            };

            _context.Prestamos.Add(prestamo);
            await _context.SaveChangesAsync();

            await _notificacionesService.CrearNotificacionParaAdminsAsync(
                "Nueva Solicitud de Préstamo", 
                $"El usuario ha solicitado {cantidad} de {item.Nombre}.", 
                codigoReserva, 
                "INFO"
            );

            return true;
        }

        public async Task<bool> SolicitarDevolucionAsync(string codigoReserva, int usuarioId)
        {
            var prestamo = await _context.Prestamos
                .FirstOrDefaultAsync(p => p.CodigoReserva == codigoReserva && p.UsuarioId == usuarioId);

            if (prestamo == null || prestamo.Estado != "APROBADO")
                return false;

            prestamo.Estado = "PENDIENTE_DEVOLUCION";
            await _context.SaveChangesAsync();
            
            await _notificacionesService.CrearNotificacionParaAdminsAsync(
                "Solicitud de Devolución", 
                $"Se ha solicitado la devolución de la reserva {codigoReserva}.", 
                codigoReserva, 
                "INFO"
            );

            return true;
        }

        public async Task<IEnumerable<Prestamo>> ObtenerSolicitudesPendientesAsync()
        {
            return await _context.Prestamos
                .Include(p => p.Usuario)
                .Include(p => p.Item)
                .Where(p => p.Estado == "PENDIENTE")
                .OrderByDescending(p => p.FechaSolicitud)
                .ToListAsync();
        }

        public async Task<IEnumerable<Prestamo>> ObtenerSolicitudesDevolucionAsync()
        {
            return await _context.Prestamos
                .Include(p => p.Usuario)
                .Include(p => p.Item)
                .Where(p => p.Estado == "PENDIENTE_DEVOLUCION")
                .OrderByDescending(p => p.FechaSolicitud)
                .ToListAsync();
        }

        public async Task<bool> ActualizarEstadoSolicitudAsync(string codigoReserva, string nuevoEstado, string? comentarioAdmin)
        {
            var prestamo = await _context.Prestamos
                .Include(p => p.Item)
                .FirstOrDefaultAsync(p => p.CodigoReserva == codigoReserva);

            if (prestamo == null)
                return false;

            prestamo.Estado = nuevoEstado;
            prestamo.ComentarioAdmin = comentarioAdmin;

            if (nuevoEstado == "APROBADO")
            {
                prestamo.FechaAprobacion = DateTime.UtcNow;
            }
            else if (nuevoEstado == "RECHAZADO")
            {
                // Devolver el stock
                if (prestamo.Item != null)
                {
                    prestamo.Item.Stock += prestamo.Cantidad;
                }
            }

            await _context.SaveChangesAsync();

            string mensaje = nuevoEstado == "APROBADO" 
                ? $"Tu préstamo {codigoReserva} ha sido aprobado." 
                : $"Tu préstamo {codigoReserva} ha sido rechazado.";
            
            await _notificacionesService.CrearNotificacionAsync(
                prestamo.UsuarioId,
                nuevoEstado == "APROBADO" ? "Préstamo Aprobado" : "Préstamo Rechazado",
                mensaje,
                codigoReserva,
                "INFO"
            );

            return true;
        }

        public async Task<IEnumerable<Prestamo>> ObtenerTodosPrestamosAsync()
        {
            return await _context.Prestamos
                .Include(p => p.Usuario)
                .Include(p => p.Item)
                .OrderByDescending(p => p.FechaSolicitud)
                .ToListAsync();
        }

        public async Task<Prestamo?> ObtenerPrestamoPorCodigoAsync(string codigoReserva)
        {
            return await _context.Prestamos
                .Include(p => p.Usuario)
                .Include(p => p.Item)
                .FirstOrDefaultAsync(p => p.CodigoReserva == codigoReserva);
        }

        public async Task<bool> MarcarComoDevueltoAsync(string codigoReserva, string? comentarioAdmin, int cantidadDefectuosa = 0, string? evidenciaUrl = null)
        {
            var prestamo = await _context.Prestamos
                .Include(p => p.Item)
                .FirstOrDefaultAsync(p => p.CodigoReserva == codigoReserva);

            if (prestamo == null || (prestamo.Estado != "APROBADO" && prestamo.Estado != "PENDIENTE_DEVOLUCION"))
                return false;

            if (cantidadDefectuosa < 0 || cantidadDefectuosa > prestamo.Cantidad)
                return false;

            prestamo.Estado = "DEVUELTO";
            if (!string.IsNullOrEmpty(evidenciaUrl))
            {
                prestamo.EvidenciaUrl = evidenciaUrl;
            }
            
            if (!string.IsNullOrEmpty(comentarioAdmin) || cantidadDefectuosa > 0)
            {
                string extraInfo = cantidadDefectuosa > 0 ? $" ({cantidadDefectuosa} Defectuosos)" : "";
                string commentBody = !string.IsNullOrEmpty(comentarioAdmin) ? comentarioAdmin : "Sin comentario extra.";
                
                prestamo.ComentarioAdmin = string.IsNullOrEmpty(prestamo.ComentarioAdmin) 
                    ? $"Devolución{extraInfo}: {commentBody}"
                    : prestamo.ComentarioAdmin + $" | Devolución{extraInfo}: {commentBody}";
            }

            // Devolver el stock SOLO para los elementos en buen estado
            if (prestamo.Item != null)
            {
                int cantidadBuena = prestamo.Cantidad - cantidadDefectuosa;
                if (cantidadBuena > 0)
                {
                    prestamo.Item.Stock += cantidadBuena;
                }
                
                if (cantidadDefectuosa > 0)
                {
                    prestamo.Item.StockDefectuoso += cantidadDefectuosa;
                }
            }

            await _context.SaveChangesAsync();

            await _notificacionesService.CrearNotificacionAsync(
                prestamo.UsuarioId,
                "Devolución Aceptada",
                $"Tu devolución de la reserva {codigoReserva} fue aceptada por el administrador.",
                codigoReserva,
                "INFO"
            );

            return true;
        }

        public async Task<bool> RechazarDevolucionAsync(string codigoReserva, string? comentarioAdmin, string? evidenciaUrl = null)
        {
            var prestamo = await _context.Prestamos
                .FirstOrDefaultAsync(p => p.CodigoReserva == codigoReserva);

            if (prestamo == null || prestamo.Estado != "PENDIENTE_DEVOLUCION")
                return false;

            // Se rechaza la devolución, el ítem vuelve a ser responsabilidad del estudiante.
            prestamo.Estado = "APROBADO";
            if (!string.IsNullOrEmpty(evidenciaUrl))
            {
                prestamo.EvidenciaUrl = evidenciaUrl;
            }
            
            if (!string.IsNullOrEmpty(comentarioAdmin))
            {
                prestamo.ComentarioAdmin = string.IsNullOrEmpty(prestamo.ComentarioAdmin) 
                    ? $"Devolución Rechazada: {comentarioAdmin}"
                    : prestamo.ComentarioAdmin + $" | Devolución Rechazada: {comentarioAdmin}";
            }

            await _context.SaveChangesAsync();

            await _notificacionesService.CrearNotificacionAsync(
                prestamo.UsuarioId,
                "Devolución Rechazada",
                $"Tu solicitud de devolución para la reserva {codigoReserva} fue rechazada.",
                codigoReserva,
                "ALERTA"
            );

            return true;
        }
    }
}

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

        public async Task<bool> SolicitarPrestamoBatchAsync(int usuarioId, System.Collections.Generic.List<(string nombreItem, int cantidad)> items, DateTime? fechaDevolucion = null)
        {
            if (items == null || items.Count == 0) return false;

            string codigoReserva = "RES-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + new Random().Next(100, 999);
            bool allSuccessful = true;
            int itemsProcesados = 0;

            foreach (var req in items)
            {
                if (req.cantidad <= 0) continue;

                var item = await _context.ItemsInventario
                    .FirstOrDefaultAsync(i => i.Nombre == req.nombreItem && i.Activo == 1 && i.EstadoOperativo == "OPERATIVO" && i.Stock >= req.cantidad);

                if (item != null)
                {
                    item.Stock -= req.cantidad;

                    var prestamo = new Prestamo
                    {
                        UsuarioId = usuarioId,
                        ItemId = item.ItemId,
                        Cantidad = req.cantidad,
                        Estado = "PENDIENTE",
                        CodigoReserva = codigoReserva,
                        FechaSolicitud = DateTime.UtcNow,
                        FechaDevolucion = fechaDevolucion
                    };

                    _context.Prestamos.Add(prestamo);
                    itemsProcesados++;
                }
                else
                {
                    allSuccessful = false;
                }
            }

            if (itemsProcesados == 0) return false;

            await _context.SaveChangesAsync();

            await _notificacionesService.CrearNotificacionParaAdminsAsync(
                "Nueva Solicitud Múltiple", 
                $"El usuario ha solicitado {itemsProcesados} tipos de elementos en lote.", 
                codigoReserva, 
                "INFO"
            );

            return allSuccessful;
        }

        public async Task<bool> SolicitarDevolucionAsync(string codigoReserva, int usuarioId, string? evidenciaUrl = null, string? comentarioEstudiante = null)
        {
            var prestamos = await _context.Prestamos
                .Where(p => p.CodigoReserva == codigoReserva && p.UsuarioId == usuarioId)
                .ToListAsync();

            if (!prestamos.Any() || prestamos.Any(p => p.Estado != "APROBADO"))
                return false;

            foreach (var prestamo in prestamos)
            {
                prestamo.Estado = "PENDIENTE_DEVOLUCION";
                if (!string.IsNullOrEmpty(evidenciaUrl))
                {
                    prestamo.EvidenciaUrl = evidenciaUrl;
                }
                
                if (!string.IsNullOrEmpty(comentarioEstudiante))
                {
                    prestamo.ComentarioAdmin = string.IsNullOrEmpty(prestamo.ComentarioAdmin) 
                        ? $"Estudiante: {comentarioEstudiante}" 
                        : prestamo.ComentarioAdmin + $" | Estudiante: {comentarioEstudiante}";
                }
            }
            
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
            var prestamos = await _context.Prestamos
                .Include(p => p.Item)
                .Where(p => p.CodigoReserva == codigoReserva)
                .ToListAsync();

            if (prestamos == null || !prestamos.Any())
                return false;

            foreach (var prestamo in prestamos)
            {
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
            }

            await _context.SaveChangesAsync();

            var primerPrestamo = prestamos.First();
            string mensaje = nuevoEstado == "APROBADO" 
                ? $"Tu préstamo {codigoReserva} ha sido aprobado." 
                : $"Tu préstamo {codigoReserva} ha sido rechazado.";
            
            await _notificacionesService.CrearNotificacionAsync(
                primerPrestamo.UsuarioId,
                nuevoEstado == "APROBADO" ? "Préstamo Aprobado" : "Préstamo Rechazado",
                mensaje,
                codigoReserva,
                "INFO"
            );

            return true;
        }

        public async Task<bool> ActualizarEstadoSolicitudPorIdAsync(int prestamoId, string nuevoEstado, string? comentarioAdmin)
        {
            var prestamo = await _context.Prestamos
                .Include(p => p.Item)
                .FirstOrDefaultAsync(p => p.PrestamoId == prestamoId);

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
                ? $"Tu ítem {prestamo.Item?.Nombre} ha sido aprobado." 
                : $"Tu ítem {prestamo.Item?.Nombre} ha sido rechazado.";
            
            await _notificacionesService.CrearNotificacionAsync(
                prestamo.UsuarioId,
                nuevoEstado == "APROBADO" ? "Ítem Aprobado" : "Ítem Rechazado",
                mensaje,
                prestamo.CodigoReserva,
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
            var prestamos = await _context.Prestamos
                .Include(p => p.Item)
                .Where(p => p.CodigoReserva == codigoReserva)
                .ToListAsync();

            if (prestamos == null || !prestamos.Any() || prestamos.Any(p => p.Estado != "APROBADO" && p.Estado != "PENDIENTE_DEVOLUCION"))
                return false;

            int defectuosasRestantes = cantidadDefectuosa;

            foreach (var prestamo in prestamos)
            {
                prestamo.Estado = "DEVUELTO";
                if (!string.IsNullOrEmpty(evidenciaUrl))
                {
                    prestamo.EvidenciaUrl = evidenciaUrl;
                }

                int defectuosasEsteItem = Math.Min(defectuosasRestantes, prestamo.Cantidad);
                defectuosasRestantes -= defectuosasEsteItem;

                if (!string.IsNullOrEmpty(comentarioAdmin) || defectuosasEsteItem > 0)
                {
                    string extraInfo = defectuosasEsteItem > 0 ? $" ({defectuosasEsteItem} Defectuosos)" : "";
                    string commentBody = !string.IsNullOrEmpty(comentarioAdmin) ? comentarioAdmin : "Sin comentario extra.";
                    
                    prestamo.ComentarioAdmin = string.IsNullOrEmpty(prestamo.ComentarioAdmin) 
                        ? $"Devolución{extraInfo}: {commentBody}"
                        : prestamo.ComentarioAdmin + $" | Devolución{extraInfo}: {commentBody}";
                }

                if (prestamo.Item != null)
                {
                    int cantidadBuena = prestamo.Cantidad - defectuosasEsteItem;
                    if (cantidadBuena > 0)
                    {
                        prestamo.Item.Stock += cantidadBuena;
                    }
                    
                    if (defectuosasEsteItem > 0)
                    {
                        prestamo.Item.StockDefectuoso += defectuosasEsteItem;
                    }
                }
            }

            await _context.SaveChangesAsync();

            var primer = prestamos.First();
            await _notificacionesService.CrearNotificacionAsync(
                primer.UsuarioId,
                "Devolución Registrada",
                $"Tu préstamo con código {codigoReserva} ha sido registrado como devuelto.",
                codigoReserva,
                "INFO"
            );

            return true;
        }

        public async Task<bool> MarcarComoDevueltoPorIdAsync(int prestamoId, string? comentarioAdmin, int cantidadDefectuosa = 0, string? evidenciaUrl = null)
        {
            var prestamo = await _context.Prestamos
                .Include(p => p.Item)
                .FirstOrDefaultAsync(p => p.PrestamoId == prestamoId);

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
                "Devolución Registrada",
                $"Tu préstamo de {prestamo.Item?.Nombre} ha sido registrado como devuelto.",
                prestamo.CodigoReserva,
                "INFO"
            );

            return true;
        }

        public async Task<bool> RechazarDevolucionAsync(string codigoReserva, string? comentarioAdmin, string? evidenciaUrl = null)
        {
            var prestamos = await _context.Prestamos
                .Where(p => p.CodigoReserva == codigoReserva)
                .ToListAsync();

            if (prestamos == null || !prestamos.Any() || prestamos.Any(p => p.Estado != "PENDIENTE_DEVOLUCION"))
                return false;

            foreach (var prestamo in prestamos)
            {
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
            }

            await _context.SaveChangesAsync();

            var primer = prestamos.First();
            await _notificacionesService.CrearNotificacionAsync(
                primer.UsuarioId,
                "Devolución Rechazada",
                $"Tu solicitud de devolución para la reserva {codigoReserva} fue rechazada.",
                codigoReserva,
                "ALERTA"
            );

            return true;
        }

        public async Task<bool> RechazarDevolucionPorIdAsync(int prestamoId, string? comentarioAdmin, string? evidenciaUrl = null)
        {
            var prestamo = await _context.Prestamos
                .Include(p => p.Item)
                .FirstOrDefaultAsync(p => p.PrestamoId == prestamoId);

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
                $"Tu solicitud de devolución para el ítem {prestamo.Item?.Nombre} fue rechazada.",
                prestamo.CodigoReserva,
                "ALERTA"
            );

            return true;
        }
    }
}

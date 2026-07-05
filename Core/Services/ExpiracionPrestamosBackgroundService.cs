using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using LaboratorioPUCE.Core.Interfaces;
using LaboratorioPUCE.Data;

namespace LaboratorioPUCE.Core.Services
{
    public class ExpiracionPrestamosBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpiracionPrestamosBackgroundService> _logger;

        public ExpiracionPrestamosBackgroundService(IServiceProvider serviceProvider, ILogger<ExpiracionPrestamosBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de Expiración de Préstamos iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await VerificarExpiracionesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al verificar expiración de préstamos.");
                }

                // Esperar 1 hora antes de volver a revisar (3600000 ms)
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task VerificarExpiracionesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LaboratorioContext>();
            var notificacionesService = scope.ServiceProvider.GetRequiredService<INotificacionesService>();

            var ahora = DateTime.UtcNow;

            // Obtener préstamos activos que tengan fecha de devolución
            var prestamosActivos = await context.Prestamos
                .Include(p => p.Item)
                .Where(p => p.Estado == "APROBADO" && p.FechaDevolucion != null)
                .ToListAsync();

            foreach (var prestamo in prestamosActivos)
            {
                if (prestamo.FechaDevolucion == null) continue;

                var fechaDevolucion = prestamo.FechaDevolucion.Value;

                // 1. Ya expiró
                if (ahora > fechaDevolucion)
                {
                    // Verificar si ya le enviamos una notificación de expiración recientemente (para no spammear cada hora)
                    // Buscaremos si tiene notificación en las últimas 24 horas
                    var limite = ahora.AddHours(-24);
                    var yaNotificadoExpirado = await context.Notificaciones.AnyAsync(n => 
                        n.UsuarioId == prestamo.UsuarioId && 
                        n.ReferenciaId == prestamo.CodigoReserva && 
                        n.Tipo == "EXPIRADO" &&
                        n.FechaCreacion > limite);

                    if (!yaNotificadoExpirado)
                    {
                        // Notificar al estudiante
                        await notificacionesService.CrearNotificacionAsync(
                            prestamo.UsuarioId,
                            "Préstamo Expirado",
                            $"El préstamo {prestamo.CodigoReserva} ({prestamo.Item?.Nombre}) ha expirado. Por favor, devuélvelo inmediatamente.",
                            prestamo.CodigoReserva,
                            "EXPIRADO"
                        );

                        // Notificar a admins
                        await notificacionesService.CrearNotificacionParaAdminsAsync(
                            "Préstamo Expirado",
                            $"El préstamo {prestamo.CodigoReserva} del estudiante ha expirado y no ha sido devuelto.",
                            prestamo.CodigoReserva,
                            "EXPIRADO"
                        );
                    }
                }
                // 2. Por expirar (en menos de 24 horas)
                else if ((fechaDevolucion - ahora).TotalHours <= 24)
                {
                    var yaNotificadoPorExpirar = await context.Notificaciones.AnyAsync(n => 
                        n.UsuarioId == prestamo.UsuarioId && 
                        n.ReferenciaId == prestamo.CodigoReserva && 
                        n.Tipo == "POR_EXPIRAR");

                    if (!yaNotificadoPorExpirar)
                    {
                        await notificacionesService.CrearNotificacionAsync(
                            prestamo.UsuarioId,
                            "Préstamo por Expirar",
                            $"El préstamo {prestamo.CodigoReserva} ({prestamo.Item?.Nombre}) expirará en menos de 24 horas.",
                            prestamo.CodigoReserva,
                            "POR_EXPIRAR"
                        );
                    }
                }
            }
        }
    }
}

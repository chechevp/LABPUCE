using System.Collections.Generic;
using System.Threading.Tasks;
using LaboratorioPUCE.Core.Entities;

namespace LaboratorioPUCE.Core.Interfaces
{
    public interface IPrestamosService
    {
        Task<bool> SolicitarPrestamoAsync(int usuarioId, string nombreItem, int cantidad, DateTime? fechaDevolucion = null);
        Task<bool> SolicitarDevolucionAsync(string codigoReserva, int usuarioId);
        Task<IEnumerable<Prestamo>> ObtenerSolicitudesPendientesAsync();
        Task<IEnumerable<Prestamo>> ObtenerSolicitudesDevolucionAsync();
        Task<bool> ActualizarEstadoSolicitudAsync(string codigoReserva, string nuevoEstado, string? comentarioAdmin);
        Task<IEnumerable<Prestamo>> ObtenerTodosPrestamosAsync();
        Task<Prestamo?> ObtenerPrestamoPorCodigoAsync(string codigoReserva);
        Task<bool> MarcarComoDevueltoAsync(string codigoReserva, string? comentarioAdmin, int cantidadDefectuosa = 0, string? evidenciaUrl = null);
        Task<bool> RechazarDevolucionAsync(string codigoReserva, string? comentarioAdmin, string? evidenciaUrl = null);
    }
}

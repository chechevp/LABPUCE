using System.Collections.Generic;
using System.Threading.Tasks;
using LaboratorioPUCE.Core.Entities;

namespace LaboratorioPUCE.Core.Interfaces
{
    public interface INotificacionesService
    {
        Task<bool> CrearNotificacionAsync(int usuarioId, string titulo, string mensaje, string? referenciaId = null, string? tipo = null);
        Task<bool> CrearNotificacionParaAdminsAsync(string titulo, string mensaje, string? referenciaId = null, string? tipo = null);
        Task<IEnumerable<Notificacion>> ObtenerNotificacionesAsync(int usuarioId);
        Task<int> ObtenerNotificacionesNoLeidasCountAsync(int usuarioId);
        Task<bool> MarcarComoLeidaAsync(int notificacionId, int usuarioId);
        Task<bool> MarcarTodasComoLeidasAsync(int usuarioId);
    }
}

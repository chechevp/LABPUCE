using System.Collections.Generic;
using System.Threading.Tasks;
using LaboratorioPUCE.Core.DTOs;

namespace LaboratorioPUCE.Core.Interfaces
{
    public interface IConsultasService
    {
        Task<IEnumerable<CatalogoItemDto>> ObtenerCatalogoDisponibleAsync(string busqueda);
    }
}

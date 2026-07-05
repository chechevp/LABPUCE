using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LaboratorioPUCE.Core.Interfaces;
using LaboratorioPUCE.Core.DTOs;
using LaboratorioPUCE.Data;

namespace LaboratorioPUCE.Core.Services
{
    public class ConsultasService : IConsultasService
    {
        private readonly LaboratorioContext _context;

        public ConsultasService(LaboratorioContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CatalogoItemDto>> ObtenerCatalogoDisponibleAsync(string busqueda)
        {
            var query = _context.ItemsInventario
                .Where(i => i.Activo == 1 && i.EsPublico == 1 && i.EstadoOperativo == "OPERATIVO" && i.EstadoPrestamo == "DISPONIBLE");

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var lowerBusqueda = busqueda.ToLower();
                query = query.Where(i => 
                    i.Nombre.ToLower().Contains(lowerBusqueda) || 
                    (i.Descripcion != null && i.Descripcion.ToLower().Contains(lowerBusqueda)) ||
                    (i.Modelo != null && i.Modelo.ToLower().Contains(lowerBusqueda)));
            }

            var catalogo = await query
                .Select(i => new CatalogoItemDto
                {
                    Nombre = i.Nombre,
                    Modelo = i.Modelo ?? "",
                    Descripcion = i.Descripcion ?? "",
                    StockDisponible = i.Stock,
                    ImagenUrl = i.ImagenUrl
                })
                .ToListAsync();

            return catalogo;
        }
    }
}

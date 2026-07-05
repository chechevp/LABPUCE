namespace LaboratorioPUCE.Core.DTOs
{
    public class CatalogoItemDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int StockDisponible { get; set; }
        public string? ImagenUrl { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaboratorioPUCE.Models; // For referencing Usuario and ItemInventario

namespace LaboratorioPUCE.Core.Entities
{
    [Table("prestamo")]
    public class Prestamo
    {
        [Key]
        [Column("prestamo_id")]
        public int PrestamoId { get; set; }

        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        [Column("item_id")]
        public int ItemId { get; set; }

        [ForeignKey(nameof(ItemId))]
        public ItemInventario? Item { get; set; }

        [Column("cantidad")]
        public int Cantidad { get; set; } = 1;

        [Required]
        [Column("estado")]
        [StringLength(50)]
        public string Estado { get; set; } = "PENDIENTE"; // PENDIENTE, APROBADO, RECHAZADO, DEVUELTO

        [Required]
        [Column("codigo_reserva")]
        [StringLength(20)]
        public string CodigoReserva { get; set; } = string.Empty;

        [Column("comentario_admin")]
        [StringLength(500)]
        public string? ComentarioAdmin { get; set; }

        [Column("fecha_solicitud")]
        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

        [Column("fecha_aprobacion")]
        public DateTime? FechaAprobacion { get; set; }

        [Column("fecha_devolucion")]
        public DateTime? FechaDevolucion { get; set; }

        [Column("evidencia_url")]
        [StringLength(500)]
        public string? EvidenciaUrl { get; set; }
    }
}

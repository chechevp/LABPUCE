using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaboratorioPUCE.Models; // For Usuario

namespace LaboratorioPUCE.Core.Entities
{
    [Table("notificacion")]
    public class Notificacion
    {
        [Key]
        [Column("notificacion_id")]
        public int NotificacionId { get; set; }

        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        [Required]
        [Column("titulo")]
        [StringLength(100)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [Column("mensaje")]
        [StringLength(500)]
        public string Mensaje { get; set; } = string.Empty;

        [Column("leida")]
        public byte Leida { get; set; } = 0;

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("referencia_id")]
        [StringLength(50)]
        public string? ReferenciaId { get; set; }

        [Column("tipo")]
        [StringLength(30)]
        public string? Tipo { get; set; } // Ej: INFO, ALERTA, EXPIRACION
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaboratorioPUCE.Models
{
    [Table("rol")]
    public class Rol
    {
        [Key]
        [Column("rol_id")]
        public int RolId { get; set; }

        [Required]
        [Column("nombre")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        [StringLength(255)]
        public string? Descripcion { get; set; }

        [Column("nivel_acceso")]
        public byte NivelAcceso { get; set; }

        [Column("activo")]
        public byte Activo { get; set; } = 1;

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }

    [Table("usuario")]
    public class Usuario
    {
        [Key]
        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [Required]
        [Column("correo")]
        [StringLength(150)]
        public string Correo { get; set; } = string.Empty;

        [Required]
        [Column("nombre")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Column("apellido")]
        [StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Column("cedula")]
        [StringLength(10)]
        public string? Cedula { get; set; }

        [Column("rol_id")]
        public int RolId { get; set; }

        [ForeignKey(nameof(RolId))]
        public Rol? Rol { get; set; }

        [Column("carrera_materia")]
        [StringLength(150)]
        public string? CarreraMateria { get; set; }

        [Column("activo")]
        public byte Activo { get; set; } = 1;

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("ultimo_acceso")]
        public DateTime? UltimoAcceso { get; set; }

        [Column("password_hash")]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty; // Added for security
    }

    [Table("taller")]
    public class Taller
    {
        [Key]
        [Column("taller_id")]
        public int TallerId { get; set; }

        [Required]
        [Column("nombre")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Column("ubicacion")]
        [StringLength(100)]
        public string? Ubicacion { get; set; }

        [Column("activo")]
        public byte Activo { get; set; } = 1;
    }

    [Table("tipoespacio")]
    public class TipoEspacio
    {
        [Key]
        [Column("tipo_espacio_id")]
        public int TipoEspacioId { get; set; }

        [Required]
        [Column("nombre")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Column("tiene_inventario")]
        public byte TieneInventario { get; set; }

        [Column("tiene_maquinaria")]
        public byte TieneMaquinaria { get; set; }

        [Column("tiene_tickets")]
        public byte TieneTickets { get; set; }
    }

    [Table("espacio")]
    public class Espacio
    {
        [Key]
        [Column("espacio_id")]
        public int EspacioId { get; set; }

        [Column("taller_id")]
        public int TallerId { get; set; }

        [ForeignKey(nameof(TallerId))]
        public Taller? Taller { get; set; }

        [Column("tipo_espacio_id")]
        public int TipoEspacioId { get; set; }

        [ForeignKey(nameof(TipoEspacioId))]
        public TipoEspacio? TipoEspacio { get; set; }

        [Required]
        [Column("nombre")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Column("capacidad")]
        public short Capacidad { get; set; }

        [Column("activo")]
        public byte Activo { get; set; } = 1;

        [Column("requiere_aprobacion")]
        public byte RequiereAprobacion { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    [Table("categoriaitem")]
    public class CategoriaItem
    {
        [Key]
        [Column("categoria_id")]
        public int CategoriaId { get; set; }

        [Required]
        [Column("nombre")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Column("es_consumible")]
        public byte EsConsumible { get; set; }

        [Column("activo")]
        public byte Activo { get; set; } = 1;

        [Column("stock_minimo")]
        public int StockMinimo { get; set; } = 1;
    }

    [Table("iteminventario")]
    public class ItemInventario
    {
        [Key]
        [Column("item_id")]
        public int ItemId { get; set; }

        [Column("espacio_id")]
        public int EspacioId { get; set; }

        [ForeignKey(nameof(EspacioId))]
        public Espacio? Espacio { get; set; }

        [Column("categoria_id")]
        public int CategoriaId { get; set; }

        [ForeignKey(nameof(CategoriaId))]
        public CategoriaItem? Categoria { get; set; }

        [Required]
        [Column("codigo_activo")]
        [StringLength(50)]
        public string CodigoActivo { get; set; } = string.Empty;

        [Required]
        [Column("nombre")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Column("marca")]
        [StringLength(100)]
        public string? Marca { get; set; }

        [Column("modelo")]
        [StringLength(100)]
        public string? Modelo { get; set; }

        [Column("numero_serie")]
        [StringLength(100)]
        public string? NumeroSerie { get; set; }

        [Column("es_maquinaria")]
        public byte EsMaquinaria { get; set; }

        [Required]
        [Column("estado_operativo")]
        [StringLength(30)]
        public string EstadoOperativo { get; set; } = "OPERATIVO"; // E.g., OPERATIVO, MANTENIMIENTO, DADO_DE_BAJA

        [Required]
        [Column("estado_prestamo")]
        [StringLength(20)]
        public string EstadoPrestamo { get; set; } = "DISPONIBLE"; // E.g., DISPONIBLE, PRESTADO, RESERVADO

        [Column("fecha_adquisicion")]
        public DateTime? FechaAdquisicion { get; set; }

        [Column("observaciones")]
        [StringLength(500)]
        public string? Observaciones { get; set; }

        [Column("activo")]
        public byte Activo { get; set; } = 1;

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("stock")]
        public int Stock { get; set; } = 1;

        [Column("stock_minimo")]
        public int StockMinimo { get; set; } = 1;

        [Column("es_publico")]
        public int EsPublico { get; set; } = 1;

        [Column("stock_defectuoso")]
        public int StockDefectuoso { get; set; } = 0;

        [Column("imagen_url")]
        [StringLength(500)]
        public string? ImagenUrl { get; set; }
    }

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
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [Column("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [Column("leida")]
        public int Leida { get; set; } = 0; // 0 = false, 1 = true

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("referencia_id")]
        public string? ReferenciaId { get; set; }

        [Column("tipo")]
        public string? Tipo { get; set; }
    }

    [Table("logtransaccion")]
    public class LogTransaccion
    {
        [Key]
        [Column("log_id")]
        public int LogId { get; set; }

        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        [Required]
        [Column("accion")]
        [StringLength(50)]
        public string Accion { get; set; } = string.Empty; // e.g. AGREGAR_STOCK, ELIMINAR_ITEM, CREAR_ITEM

        [Required]
        [Column("detalle")]
        public string Detalle { get; set; } = string.Empty; // Justification or specific details

        [Column("referencia_id")]
        public int? ReferenciaId { get; set; } // e.g. ItemId

        [Column("fecha_hora")]
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;
    }

    [Table("sesionusuario")]
    public class SesionUsuario
    {
        [Key]
        [Column("sesion_id")]
        [StringLength(36)]
        public string SesionId { get; set; } = Guid.NewGuid().ToString();

        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        [Required]
        [Column("token")]
        [StringLength(512)]
        public string Token { get; set; } = string.Empty;

        [Column("fecha_inicio")]
        public DateTime FechaInicio { get; set; } = DateTime.UtcNow;

        [Column("fecha_expira")]
        public DateTime FechaExpira { get; set; }

        [Column("ip_origen")]
        [StringLength(45)]
        public string? IpOrigen { get; set; }

        [Column("activa")]
        public byte Activa { get; set; } = 1;
    }
}

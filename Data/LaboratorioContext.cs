using Microsoft.EntityFrameworkCore;
using LaboratorioPUCE.Models;
using System.Security.Cryptography;
using System.Text;

namespace LaboratorioPUCE.Data
{
    public class LaboratorioContext : DbContext
    {
        public LaboratorioContext(DbContextOptions<LaboratorioContext> options) : base(options)
        {
        }

        public DbSet<Rol> Roles => Set<Rol>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Taller> Talleres => Set<Taller>();
        public DbSet<TipoEspacio> TiposEspacio => Set<TipoEspacio>();
        public DbSet<Espacio> Espacios => Set<Espacio>();
        public DbSet<CategoriaItem> CategoriasItem => Set<CategoriaItem>();
        public DbSet<ItemInventario> ItemsInventario => Set<ItemInventario>();
        public DbSet<SesionUsuario> SesionesUsuario => Set<SesionUsuario>();
        public DbSet<LaboratorioPUCE.Core.Entities.Prestamo> Prestamos => Set<LaboratorioPUCE.Core.Entities.Prestamo>();
        public DbSet<LaboratorioPUCE.Core.Entities.Notificacion> Notificaciones => Set<LaboratorioPUCE.Core.Entities.Notificacion>();
        public DbSet<LaboratorioPUCE.Models.LogTransaccion> LogsTransacciones => Set<LaboratorioPUCE.Models.LogTransaccion>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure unique keys or indices if required
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Correo)
                .IsUnique();

            modelBuilder.Entity<ItemInventario>()
                .HasIndex(i => i.CodigoActivo)
                .IsUnique();

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Roles
            modelBuilder.Entity<Rol>().HasData(
                new Rol { RolId = 1, Nombre = "Administrador", Descripcion = "Control operativo total y administración", NivelAcceso = 10, Activo = 1 },
                new Rol { RolId = 2, Nombre = "Estudiante", Descripcion = "Consulta pública y reserva virtual", NivelAcceso = 1, Activo = 1 },
                new Rol { RolId = 3, Nombre = "Docente", Descripcion = "Aprobar/rechazar devoluciones e historial", NivelAcceso = 5, Activo = 1 }
            );

            // Hash passwords for seed users
            var adminPasswordHash = HashPassword("AdminPass123!");
            var studentPasswordHash = HashPassword("StudentPass123!");
            var docentePasswordHash = HashPassword("DocentePass123!");

            // Seed Users
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    UsuarioId = 1,
                    Correo = "admin@pucesa.edu.ec",
                    Nombre = "Juan",
                    Apellido = "Pérez",
                    Cedula = "1801234567",
                    RolId = 1,
                    CarreraMateria = "Sistemas",
                    Activo = 1,
                    FechaCreacion = new DateTime(2026, 6, 1),
                    PasswordHash = adminPasswordHash
                },
                new Usuario
                {
                    UsuarioId = 2,
                    Correo = "estudiante@pucesa.edu.ec",
                    Nombre = "Carlos",
                    Apellido = "Mena",
                    Cedula = "1807654321",
                    RolId = 2,
                    CarreraMateria = "Tecnologías de la Información",
                    Activo = 1,
                    FechaCreacion = new DateTime(2026, 6, 1),
                    PasswordHash = studentPasswordHash
                },
                new Usuario
                {
                    UsuarioId = 3,
                    Correo = "docente@pucesa.edu.ec",
                    Nombre = "María",
                    Apellido = "Gómez",
                    Cedula = "1809876543",
                    RolId = 3,
                    CarreraMateria = "Sistemas",
                    Activo = 1,
                    FechaCreacion = new DateTime(2026, 6, 1),
                    PasswordHash = docentePasswordHash
                }
            );

            // Seed Talleres
            modelBuilder.Entity<Taller>().HasData(
                new Taller { TallerId = 1, Nombre = "Taller de Electrónica", Ubicacion = "Edificio A - Aula 102", Activo = 1 }
            );

            // Seed TipoEspacio
            modelBuilder.Entity<TipoEspacio>().HasData(
                new TipoEspacio { TipoEspacioId = 1, Nombre = "Laboratorio Físico", TieneInventario = 1, TieneMaquinaria = 1, TieneTickets = 1 }
            );

            // Seed Espacio
            modelBuilder.Entity<Espacio>().HasData(
                new Espacio
                {
                    EspacioId = 1,
                    TallerId = 1,
                    TipoEspacioId = 1,
                    Nombre = "Laboratorio de Microcontroladores",
                    Descripcion = "Espacio destinado a prácticas de sistemas embebidos y circuitos.",
                    Capacidad = 25,
                    Activo = 1,
                    RequiereAprobacion = 1,
                    FechaCreacion = new DateTime(2026, 6, 1)
                }
            );

            // Seed CategoriaItem
            modelBuilder.Entity<CategoriaItem>().HasData(
                new CategoriaItem { CategoriaId = 1, Nombre = "Placas de Desarrollo", EsConsumible = 0 },
                new CategoriaItem { CategoriaId = 2, Nombre = "Sensores y Componentes", EsConsumible = 1 },
                new CategoriaItem { CategoriaId = 3, Nombre = "Instrumentos de Medición", EsConsumible = 0 }
            );

            // Seed ItemsInventario
            modelBuilder.Entity<ItemInventario>().HasData(
                new ItemInventario
                {
                    ItemId = 1,
                    EspacioId = 1,
                    CategoriaId = 1,
                    CodigoActivo = "DEV-ESP32-001",
                    Nombre = "Placa ESP32 NodeMCU",
                    Descripcion = "Módulo de desarrollo Wi-Fi + Bluetooth.",
                    Marca = "Espressif",
                    Modelo = "ESP32-WROOM-32D",
                    NumeroSerie = "SN-ESP32-90812",
                    EsMaquinaria = 0,
                    EstadoOperativo = "OPERATIVO",
                    EstadoPrestamo = "DISPONIBLE",
                    FechaAdquisicion = new DateTime(2025, 1, 15),
                    Observaciones = "Incluye cable micro USB.",
                    Activo = 1,
                    FechaCreacion = new DateTime(2026, 6, 1)
                },
                new ItemInventario
                {
                    ItemId = 2,
                    EspacioId = 1,
                    CategoriaId = 3,
                    CodigoActivo = "INS-MULT-001",
                    Nombre = "Multímetro Digital Profesional",
                    Descripcion = "Medidor de voltaje, corriente y resistencia con auto-rango.",
                    Marca = "Fluke",
                    Modelo = "Fluke 115",
                    NumeroSerie = "SN-FLK115-4421",
                    EsMaquinaria = 0,
                    EstadoOperativo = "OPERATIVO",
                    EstadoPrestamo = "DISPONIBLE",
                    FechaAdquisicion = new DateTime(2024, 11, 20),
                    Observaciones = "Con estuche protector.",
                    Activo = 1,
                    FechaCreacion = new DateTime(2026, 6, 1)
                }
            );
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(hashedBytes);
        }
    }
}

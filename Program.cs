using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using LaboratorioPUCE.Data;
using System;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Core Services
builder.Services.AddScoped<LaboratorioPUCE.Core.Interfaces.IConsultasService, LaboratorioPUCE.Core.Services.ConsultasService>();
builder.Services.AddScoped<LaboratorioPUCE.Core.Interfaces.IPrestamosService, LaboratorioPUCE.Core.Services.PrestamosService>();
builder.Services.AddScoped<LaboratorioPUCE.Core.Interfaces.INotificacionesService, LaboratorioPUCE.Core.Services.NotificacionesService>();
builder.Services.AddHostedService<LaboratorioPUCE.Core.Services.ExpiracionPrestamosBackgroundService>();

// Configure DB Context (MySQL with SQLite fallback for easy local execution)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("server", StringComparison.OrdinalIgnoreCase))
{
    // MySQL configuration
    builder.Services.AddDbContext<LaboratorioContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
}
else
{
    // SQLite configuration as a robust, no-dependency local fallback
    var sqlitePath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "laboratorio.db");
    builder.Services.AddDbContext<LaboratorioContext>(options =>
        options.UseSqlite($"Data Source={sqlitePath}"));
}

var app = builder.Build();

// Auto-create and seed database (using EnsureCreated for simple setup, handles our seeded data)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LaboratorioContext>();
    db.Database.EnsureCreated();

    // Ensure Docente role and user exist for existing databases
    try
    {
        if (!db.Roles.Any(r => r.RolId == 3))
        {
            db.Roles.Add(new LaboratorioPUCE.Models.Rol { RolId = 3, Nombre = "Docente", Descripcion = "Aprobar/rechazar devoluciones e historial", NivelAcceso = 5, Activo = 1 });
            db.SaveChanges();
        }
        if (!db.Usuarios.Any(u => u.RolId == 3))
        {
            db.Usuarios.Add(new LaboratorioPUCE.Models.Usuario
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
                PasswordHash = LaboratorioContext.HashPassword("DocentePass123!")
            });
            db.SaveChanges();
        }
    }
    catch { }

    // Seed Categories with StockMinimo
    try
    {
        var cats = new[]
        {
            new { Nombre = "Consumible", Stock = 10 },
            new { Nombre = "Placas de Desarrollo", Stock = 2 },
            new { Nombre = "Sensores", Stock = 5 },
            new { Nombre = "Herramientas Generales", Stock = 3 }
        };

        foreach (var c in cats)
        {
            if (!db.CategoriasItem.Any(x => x.Nombre == c.Nombre))
            {
                db.CategoriasItem.Add(new LaboratorioPUCE.Models.CategoriaItem
                {
                    Nombre = c.Nombre,
                    StockMinimo = c.Stock,
                    Activo = 1
                });
            }
        }
        db.SaveChanges();
    }
    catch { }

    // Migrate existing SQLite DB to add the new column without dropping data
    if (db.Database.IsSqlite())
    {
        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE iteminventario ADD COLUMN stock_defectuoso INTEGER NOT NULL DEFAULT 0;");
        }
        catch { }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE categoriaitem ADD COLUMN activo INTEGER NOT NULL DEFAULT 1;");
        }
        catch { }

        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE categoriaitem ADD COLUMN stock_minimo INTEGER NOT NULL DEFAULT 1;");
        }
        catch { }

        try
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS notificacion (
                    notificacion_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    usuario_id INTEGER NOT NULL,
                    titulo TEXT NOT NULL,
                    mensaje TEXT NOT NULL,
                    leida INTEGER NOT NULL DEFAULT 0,
                    fecha_creacion TEXT NOT NULL,
                    referencia_id TEXT,
                    tipo TEXT,
                    FOREIGN KEY (usuario_id) REFERENCES usuario(usuario_id)
                );
            ");
        }
        catch { }


        try
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS logtransaccion (
                    log_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    usuario_id INTEGER NOT NULL,
                    accion TEXT NOT NULL,
                    detalle TEXT NOT NULL,
                    referencia_id INTEGER,
                    fecha_hora TEXT NOT NULL,
                    FOREIGN KEY (usuario_id) REFERENCES usuario(usuario_id)
                );
            ");
        }
        catch { }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Serve static files from wwwroot (index.html, dashboard.html, styles.css, etc.)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapControllers();

app.Run();

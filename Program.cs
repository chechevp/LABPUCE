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

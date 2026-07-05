using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LaboratorioPUCE.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categoriaitem",
                columns: table => new
                {
                    categoria_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    es_consumible = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categoriaitem", x => x.categoria_id);
                });

            migrationBuilder.CreateTable(
                name: "rol",
                columns: table => new
                {
                    rol_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    nivel_acceso = table.Column<byte>(type: "INTEGER", nullable: false),
                    activo = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol", x => x.rol_id);
                });

            migrationBuilder.CreateTable(
                name: "taller",
                columns: table => new
                {
                    taller_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ubicacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    activo = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_taller", x => x.taller_id);
                });

            migrationBuilder.CreateTable(
                name: "tipoespacio",
                columns: table => new
                {
                    tipo_espacio_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    tiene_inventario = table.Column<byte>(type: "INTEGER", nullable: false),
                    tiene_maquinaria = table.Column<byte>(type: "INTEGER", nullable: false),
                    tiene_tickets = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipoespacio", x => x.tipo_espacio_id);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    correo = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    apellido = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    cedula = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    rol_id = table.Column<int>(type: "INTEGER", nullable: false),
                    carrera_materia = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    activo = table.Column<byte>(type: "INTEGER", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ultimo_acceso = table.Column<DateTime>(type: "TEXT", nullable: true),
                    password_hash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.usuario_id);
                    table.ForeignKey(
                        name: "FK_usuario_rol_rol_id",
                        column: x => x.rol_id,
                        principalTable: "rol",
                        principalColumn: "rol_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "espacio",
                columns: table => new
                {
                    espacio_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    taller_id = table.Column<int>(type: "INTEGER", nullable: false),
                    tipo_espacio_id = table.Column<int>(type: "INTEGER", nullable: false),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    capacidad = table.Column<short>(type: "INTEGER", nullable: false),
                    activo = table.Column<byte>(type: "INTEGER", nullable: false),
                    requiere_aprobacion = table.Column<byte>(type: "INTEGER", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_espacio", x => x.espacio_id);
                    table.ForeignKey(
                        name: "FK_espacio_taller_taller_id",
                        column: x => x.taller_id,
                        principalTable: "taller",
                        principalColumn: "taller_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_espacio_tipoespacio_tipo_espacio_id",
                        column: x => x.tipo_espacio_id,
                        principalTable: "tipoespacio",
                        principalColumn: "tipo_espacio_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sesionusuario",
                columns: table => new
                {
                    sesion_id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    usuario_id = table.Column<int>(type: "INTEGER", nullable: false),
                    token = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    fecha_expira = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ip_origen = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    activa = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesionusuario", x => x.sesion_id);
                    table.ForeignKey(
                        name: "FK_sesionusuario_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "iteminventario",
                columns: table => new
                {
                    item_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    espacio_id = table.Column<int>(type: "INTEGER", nullable: false),
                    categoria_id = table.Column<int>(type: "INTEGER", nullable: false),
                    codigo_activo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    marca = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    modelo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    numero_serie = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    es_maquinaria = table.Column<byte>(type: "INTEGER", nullable: false),
                    estado_operativo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    estado_prestamo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    fecha_adquisicion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    activo = table.Column<byte>(type: "INTEGER", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    stock = table.Column<int>(type: "INTEGER", nullable: false),
                    stock_defectuoso = table.Column<int>(type: "INTEGER", nullable: false),
                    imagen_url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iteminventario", x => x.item_id);
                    table.ForeignKey(
                        name: "FK_iteminventario_categoriaitem_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categoriaitem",
                        principalColumn: "categoria_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_iteminventario_espacio_espacio_id",
                        column: x => x.espacio_id,
                        principalTable: "espacio",
                        principalColumn: "espacio_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prestamo",
                columns: table => new
                {
                    prestamo_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    usuario_id = table.Column<int>(type: "INTEGER", nullable: false),
                    item_id = table.Column<int>(type: "INTEGER", nullable: false),
                    cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    estado = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    codigo_reserva = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    comentario_admin = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    fecha_solicitud = table.Column<DateTime>(type: "TEXT", nullable: false),
                    fecha_aprobacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    fecha_devolucion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    evidencia_url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prestamo", x => x.prestamo_id);
                    table.ForeignKey(
                        name: "FK_prestamo_iteminventario_item_id",
                        column: x => x.item_id,
                        principalTable: "iteminventario",
                        principalColumn: "item_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prestamo_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "categoriaitem",
                columns: new[] { "categoria_id", "es_consumible", "nombre" },
                values: new object[,]
                {
                    { 1, (byte)0, "Placas de Desarrollo" },
                    { 2, (byte)1, "Sensores y Componentes" },
                    { 3, (byte)0, "Instrumentos de Medición" }
                });

            migrationBuilder.InsertData(
                table: "rol",
                columns: new[] { "rol_id", "activo", "descripcion", "nivel_acceso", "nombre" },
                values: new object[,]
                {
                    { 1, (byte)1, "Control operativo total y administración", (byte)10, "Administrador" },
                    { 2, (byte)1, "Consulta pública y reserva virtual", (byte)1, "Estudiante" }
                });

            migrationBuilder.InsertData(
                table: "taller",
                columns: new[] { "taller_id", "activo", "nombre", "ubicacion" },
                values: new object[] { 1, (byte)1, "Taller de Electrónica", "Edificio A - Aula 102" });

            migrationBuilder.InsertData(
                table: "tipoespacio",
                columns: new[] { "tipo_espacio_id", "nombre", "tiene_inventario", "tiene_maquinaria", "tiene_tickets" },
                values: new object[] { 1, "Laboratorio Físico", (byte)1, (byte)1, (byte)1 });

            migrationBuilder.InsertData(
                table: "espacio",
                columns: new[] { "espacio_id", "activo", "capacidad", "descripcion", "fecha_creacion", "nombre", "requiere_aprobacion", "taller_id", "tipo_espacio_id" },
                values: new object[] { 1, (byte)1, (short)25, "Espacio destinado a prácticas de sistemas embebidos y circuitos.", new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Laboratorio de Microcontroladores", (byte)1, 1, 1 });

            migrationBuilder.InsertData(
                table: "usuario",
                columns: new[] { "usuario_id", "activo", "apellido", "carrera_materia", "cedula", "correo", "fecha_creacion", "nombre", "password_hash", "rol_id", "ultimo_acceso" },
                values: new object[,]
                {
                    { 1, (byte)1, "Pérez", "Sistemas", "1801234567", "admin@pucesa.edu.ec", new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Juan", "A44CA5D29F6DAB4320AB986479FA985B2D584B11A7DA934F7E80BB1449913A07", 1, null },
                    { 2, (byte)1, "Mena", "Tecnologías de la Información", "1807654321", "estudiante@pucesa.edu.ec", new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Carlos", "67D93FFBC123EC5314BD8D4779E00FB74D7F8F3D7B6A2FBC8DF7F670DB45396B", 2, null }
                });

            migrationBuilder.InsertData(
                table: "iteminventario",
                columns: new[] { "item_id", "activo", "categoria_id", "codigo_activo", "descripcion", "es_maquinaria", "espacio_id", "estado_operativo", "estado_prestamo", "fecha_adquisicion", "fecha_creacion", "imagen_url", "marca", "modelo", "nombre", "numero_serie", "observaciones", "stock", "stock_defectuoso" },
                values: new object[,]
                {
                    { 1, (byte)1, 1, "DEV-ESP32-001", "Módulo de desarrollo Wi-Fi + Bluetooth.", (byte)0, 1, "OPERATIVO", "DISPONIBLE", new DateTime(2025, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Espressif", "ESP32-WROOM-32D", "Placa ESP32 NodeMCU", "SN-ESP32-90812", "Incluye cable micro USB.", 1, 0 },
                    { 2, (byte)1, 3, "INS-MULT-001", "Medidor de voltaje, corriente y resistencia con auto-rango.", (byte)0, 1, "OPERATIVO", "DISPONIBLE", new DateTime(2024, 11, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Fluke", "Fluke 115", "Multímetro Digital Profesional", "SN-FLK115-4421", "Con estuche protector.", 1, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_espacio_taller_id",
                table: "espacio",
                column: "taller_id");

            migrationBuilder.CreateIndex(
                name: "IX_espacio_tipo_espacio_id",
                table: "espacio",
                column: "tipo_espacio_id");

            migrationBuilder.CreateIndex(
                name: "IX_iteminventario_categoria_id",
                table: "iteminventario",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_iteminventario_codigo_activo",
                table: "iteminventario",
                column: "codigo_activo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iteminventario_espacio_id",
                table: "iteminventario",
                column: "espacio_id");

            migrationBuilder.CreateIndex(
                name: "IX_prestamo_item_id",
                table: "prestamo",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_prestamo_usuario_id",
                table: "prestamo",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_sesionusuario_usuario_id",
                table: "sesionusuario",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_correo",
                table: "usuario",
                column: "correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_rol_id",
                table: "usuario",
                column: "rol_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prestamo");

            migrationBuilder.DropTable(
                name: "sesionusuario");

            migrationBuilder.DropTable(
                name: "iteminventario");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "categoriaitem");

            migrationBuilder.DropTable(
                name: "espacio");

            migrationBuilder.DropTable(
                name: "rol");

            migrationBuilder.DropTable(
                name: "taller");

            migrationBuilder.DropTable(
                name: "tipoespacio");
        }
    }
}

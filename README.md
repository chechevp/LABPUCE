# Sistema de Gestión de Préstamos de Laboratorio - PUCE

Este es el repositorio del **Sistema de Gestión de Préstamos e Inventario de Laboratorio** para la **Pontificia Universidad Católica del Ecuador (PUCE)**. El sistema permite el control de inventario de equipos y materiales, el registro y aprobación de préstamos de laboratorios, alertas de stock mínimo, gestión de roles de usuario, notificaciones y auditoría de transacciones.

---

## 🚀 Arquitectura y Tecnologías

El proyecto está diseñado bajo una arquitectura desacoplada con el backend en .NET y el frontend en la carpeta de recursos estáticos:

*   **Backend:** ASP.NET Core 10.0 (Web API)
*   **Acceso a Datos:** Entity Framework Core (soporta SQLite local de forma automática y MySQL para producción)
*   **Frontend:** HTML5, CSS3 y JavaScript (Vanilla) alojado en `wwwroot`
*   **Base de Datos:** SQLite (`laboratorio.db`) para ejecución local inmediata, con compatibilidad para MySQL mediante configuración en `appsettings.json`.

---

## 📦 Estructura del Proyecto

```text
Proyecto_Laboratorio_PUCE/
│
├── LaboratorioPUCE/                   # Carpeta principal del código fuente .NET
│   ├── Controllers/                   # Controladores de la API Web (REST)
│   ├── Core/                          # Lógica de negocio, servicios e interfaces
│   │   ├── Interfaces/
│   │   └── Services/
│   ├── Data/                          # Contexto de Base de Datos (EF Core) y Semillas
│   ├── Migrations/                    # Migraciones de Entity Framework
│   ├── Models/                        # Modelos y Entidades de datos
│   ├── Properties/                    # Configuraciones de lanzamiento del proyecto
│   ├── wwwroot/                       # Archivos estáticos del Frontend (HTML, CSS, JS)
│   │   ├── css/                       # Estilos personalizados
│   │   ├── js/                        # Controladores y lógica frontend
│   │   ├── index.html                 # Página de inicio / Login
│   │   └── dashboard.html             # Panel principal
│   ├── Program.cs                     # Configuración de servicios y pipeline HTTP
│   └── appsettings.json               # Configuración del entorno y cadenas de conexión
│
└── extracted_mwb/                     # Archivos fuente del modelo de base de datos MySQL Workbench
```

---

## ⚙️ Requisitos Previos

Asegúrate de tener instalados los siguientes componentes:

*   [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
*   Un editor de código como VS Code, Visual Studio 2022 o JetBrains Rider.

---

## 🛠️ Configuración y Ejecución Local

1.  **Clonar o abrir el repositorio** en tu entorno local.
2.  **Configuración de la Base de Datos**:
    *   Por defecto, el sistema utiliza **SQLite** para facilitar el desarrollo local sin dependencias externas. Se creará automáticamente un archivo de base de datos llamado `laboratorio.db` en el directorio de `LaboratorioPUCE/` la primera vez que inicies el servidor.
    *   Si deseas conectar el proyecto a **MySQL**, edita la cadena de conexión en el archivo [appsettings.json](file:///c:/Users/chech/Desktop/Proyecto_Laboratorio_PUCE/LaboratorioPUCE/appsettings.json):
        ```json
        "ConnectionStrings": {
          "DefaultConnection": "server=localhost;port=3306;database=laboratoriopuce;user=root;password=tu_contraseña"
        }
        ```
3.  **Ejecutar la aplicación**:
    Abre una terminal en la carpeta [LaboratorioPUCE](file:///c:/Users/chech/Desktop/Proyecto_Laboratorio_PUCE/LaboratorioPUCE) y ejecuta:
    ```bash
    dotnet run
    ```
4.  **Acceder al navegador**:
    Abre tu navegador web e ingresa a la dirección indicada por la consola (generalmente `http://localhost:5000` o `https://localhost:5001`).

---

## 🔑 Credenciales por Defecto (Seed)

Al iniciar el sistema por primera vez, se sembrarán automáticamente datos de prueba en la base de datos:

*   **Docente de Prueba**:
    *   **Usuario:** `docente@pucesa.edu.ec`
    *   **Contraseña:** `DocentePass123!`
*   **Roles disponibles:**
    1.  Administrador / Encargado de Laboratorio
    2.  Estudiante
    3.  Docente (permisos para aprobar/rechazar devoluciones e historial)

---

## 🛠️ Servicios en Segundo Plano (Background Services)

La aplicación incluye un servicio hospedado en segundo plano (`ExpiracionPrestamosBackgroundService`) que monitorea periódicamente el estado de los préstamos:
*   Marca automáticamente los préstamos no devueltos a tiempo como **Expirados**.
*   Genera notificaciones de alerta automáticas para los usuarios involucrados.

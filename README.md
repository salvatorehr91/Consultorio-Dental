# Consultorio Dental

Aplicación para un consultorio dental: app móvil y de escritorio en **.NET MAUI**, API REST en **ASP.NET Core 8** y base de datos en **SQL Server**.

> Estado: en desarrollo, funcional de extremo a extremo en local. Las pantallas crean y consultan registros reales a través del API, con datos de ejemplo como respaldo si el API no responde.

## Qué hace

- **Panel del día** con las citas de hoy, buscador de pacientes y resumen.
- **Pacientes**: lista con búsqueda, alta con validación y acceso directo a su historial de pagos.
- **Citas**: lista con filtro por fecha y estado, alta de cita, cambio de estado y registro de pago desde la cita.
- **Pagos**: historial con filtros por fecha, paciente, método y estado, total de lo filtrado y comprobante por pago.
- **Comprobante**: folio `PAG-0000`, con los datos del consultorio, para ver en pantalla e imprimir desde el navegador.

## Tecnologías

| Capa | Tecnología |
| --- | --- |
| App | .NET MAUI 10 (Android, iOS, MacCatalyst, Windows), C# / XAML, Shell |
| API | ASP.NET Core 8 Web API, Entity Framework Core, Swagger |
| Base de datos | SQL Server, script `DB/ConsultorioDental.sql` |
| Comunicación | REST / JSON (camelCase), un solo `HttpClient` en `ApiService` |

## Estructura del repositorio

```
Consultorio-Dental/
├── README.md                    # Este archivo
├── DB/
│   └── ConsultorioDental.sql    # Base de datos: 6 tablas + datos de ejemplo
├── Maui-ClinicaDental/          # App MAUI (namespace Maui_ClinicaDental)
│   ├── MainPage.xaml(.cs)       # Panel del día
│   ├── PacientesPage.xaml(.cs)  # Lista y búsqueda de pacientes
│   ├── NuevoPacientePage.xaml(.cs)
│   ├── CitasPage.xaml(.cs)      # Lista de citas con filtros
│   ├── NuevaCitaPage.xaml(.cs)  # Alta de cita
│   ├── PagosPage.xaml(.cs)      # Historial de pagos y comprobante
│   ├── AppShell.xaml(.cs)       # Rutas Shell
│   ├── Models/                  # ApiModels.cs, Citas.cs, Pacientes.cs, …
│   └── Service/
│       └── ApiService.cs        # Consumo del API REST
└── ConsultorioDental.Api/       # API ASP.NET Core 8 (namespace ConsultorioDental.Api)
    ├── Program.cs
    ├── appsettings.json         # SqlConnection + datos del consultorio
    ├── Context/ConsultorioDbContext.cs
    ├── Models/Entidades.cs
    ├── Data/Dtos.cs
    └── Controllers/             # Pacientes, Dentistas, Tratamientos, Citas, Pagos, Diagnostico
```

## Base de datos

Ejecuta `DB/ConsultorioDental.sql` en SQL Server Management Studio o con `sqlcmd`. El script es idempotente: crea la base `ConsultorioDental` si no existe, crea las tablas solo si no existen y carga datos de ejemplo solo si las tablas están vacías.

Tablas: **Dentistas, Pacientes, Tratamientos, Citas, CitaTratamientos y Pagos**, con llaves foráneas e índices. Una cita tiene un paciente, un dentista, fecha y hora, duración, estado y puede incluir varios tratamientos; el precio queda guardado en `CitaTratamientos`, para que un cambio futuro en el catálogo no altere lo cobrado en citas pasadas.

Estados de cita: `Programada`, `Confirmada`, `Completada`, `Cancelada`, `NoAsistio`.
Métodos de pago: `Efectivo`, `Tarjeta`, `Transferencia`, `Otro`. Estados de pago: `Pagado`, `Pendiente`, `Reembolsado`.

Datos de ejemplo: dentistas Ana Pérez (General) y Luis Gómez (Ortodoncia); pacientes María López, Carlos Ruiz y Sofía Mendoza; tratamientos Limpieza dental ($800), Revisión general ($500) y Ortodoncia — revisión ($1200).

## API REST

Base: la URL del perfil de ejecución del API (en desarrollo, `https://localhost:65350`, según `launchSettings.json`). Swagger está activado en Development y Staging, en la raíz (`/`).

| Método | Ruta | Descripción |
| --- | --- | --- |
| GET | `/api/pacientes?buscar={texto}` | Lista de pacientes, con búsqueda |
| GET | `/api/pacientes/count` | Total de pacientes |
| GET | `/api/pacientes/{id}` | Un paciente |
| POST | `/api/pacientes` | Crear paciente |
| GET | `/api/dentistas` | Lista de dentistas |
| GET | `/api/dentistas/{id}` | Un dentista |
| POST | `/api/dentistas` | Crear dentista |
| GET | `/api/tratamientos` | Lista de tratamientos |
| GET | `/api/tratamientos/{id}` | Un tratamiento |
| POST | `/api/tratamientos` | Crear tratamiento |
| GET | `/api/citas?fecha={yyyy-MM-dd}&estado={estado}` | Lista de citas con filtros |
| GET | `/api/citas/hoy` | Citas de hoy, ordenadas por hora |
| GET | `/api/citas/{id}` | Una cita |
| POST | `/api/citas` | Crear cita (inserta en `CitaTratamientos` al precio del catálogo) |
| PUT | `/api/citas/{id}/estado` | Cambiar el estado de una cita |
| GET | `/api/pagos?citaId=&pacienteId=&desde=&hasta=&metodo=&estado=` | Historial de pagos con filtros |
| GET | `/api/pagos/{id}` | Un pago |
| GET | `/api/pagos/{id}/comprobante` | Comprobante del pago (folio `PAG-0000`) |
| POST | `/api/pagos` | Registrar pago (valida cita, monto > 0, método y estado; la fecha la pone el servidor en UTC) |
| GET | `/context/diagnostico` | Diagnóstico: servidor, base de datos y tablas que ve el API |

Dos decisiones deliberadas:

- **Registrar un pago y cambiar el estado de la cita son dos pasos separados.** Registrar el pago no marca la cita como `Completada`; el estado se cambia aparte.
- **El filtro de pagos por fecha se hace en hora local en la app.** `FechaPago` se guarda en UTC, así que el servidor amplía el rango un día a cada lado y la app compara con la fecha local.

## App MAUI

Páginas registradas en `AppShell` (`MainPage`, `NuevaCitaPage`, `NuevoPacientePage`, `PacientesPage`, `CitasPage`, `PagosPage`):

- **MainPage**: encabezado del consultorio, buscador de pacientes, resumen del día (citas de hoy y próxima cita), acciones rápidas y lista de citas de hoy.
- **PacientesPage**: lista con búsqueda y contador; al tocar un paciente se puede ver su detalle o ir a **Ver pagos**, que abre el historial con ese paciente seleccionado y el filtro bloqueado.
- **NuevoPacientePage**: formulario con validación, `POST /api/pacientes`.
- **CitasPage**: lista con filtro por fecha y estado; al tocar una cita ofrece **Ver detalle**, **Cambiar estado**, **Registrar pago** (monto y método, mostrando el total ya pagado de esa cita) y **Ver pagos**.
- **NuevaCitaPage**: paciente, dentista, fecha, hora, tratamiento opcional, duración, estado, motivo y notas; valida paciente y dentista obligatorios y que la fecha/hora no sea pasada.
- **PagosPage**: historial con filtros por fecha (desde/hasta), paciente, método y estado; total de la lista filtrada; al tocar un pago muestra su comprobante, que se puede cerrar sin perder la lista. La lista es desplazable aunque el historial sea largo.

Si el API no responde, los `GET` devuelven `null` y la app muestra los datos de ejemplo con un aviso amarillo (`AvisoApi`), en vez de romperse. Los `POST` devuelven `(false, motivo)`.

La URL base del API está en `Service/ApiService.cs`: por defecto `https://localhost:65350/` fuera de Android. Para un dispositivo físico, usa `ApiService.BaseUrlOverride` con la IP de tu PC y abre el puerto en el firewall.

## Configuración

### Cadena de conexión

`Program.cs` lee `ConnectionStrings:SqlConnection`, con `DefaultConnection` como respaldo, y falla al arrancar con un mensaje claro si no hay ninguna. En `appsettings.json` va la cadena base (localhost, `Trusted_Connection`).

Para desarrollo local, crea tu propio `appsettings.Development.json` (no se sube al repo) o, mejor, usa variable de entorno o User Secrets:

```powershell
$env:ConnectionStrings__SqlConnection = "Server=SALVATORE;Database=ConsultorioDental;User Id=logic;Password=TU_PASSWORD"
```

```powershell
cd ConsultorioDental.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:SqlConnection" "Server=SALVATORE;Database=ConsultorioDental;User Id=logic;Password=TU_PASSWORD"
```

> Nunca subas contraseñas a Git. Los archivos `appsettings.Development.json` y `appsettings.Staging.json` no están versionados.

### Datos del consultorio (comprobante)

En `appsettings.json`, sección `"Consultorio"`:

```json
"Consultorio": {
  "Nombre": "Consultorio Dental",
  "Direccion": "Domicilio del Consultorio",
  "Telefono": "55 5555 5555",
  "Email": "email@consultorio.com"
}
```

Cambia esos valores por los reales del consultorio. El API los lee con `IConfiguration` y el comprobante los muestra en pantalla y en el HTML para imprimir (Ctrl+P). MAUI no tiene API de impresión multiplataforma, por eso el comprobante se abre en el navegador o se comparte con la hoja de compartir del sistema.

## Cómo correrlo

1. **Base de datos**: ejecuta `DB/ConsultorioDental.sql` en SQL Server.
2. **API**: abre `ConsultorioDental.Api/ConsultorioDental.Api.sln` en Visual Studio, revisa la cadena de conexión y corre el proyecto. Verifica en Swagger `GET /api/citas/hoy`.
3. **App MAUI**: abre la solución de `Maui-ClinicaDental`, ponla como proyecto de inicio y corre en Windows, Android o el emulador. Si el API corre en otro puerto o en otra máquina, ajusta `ApiService.BaseUrlOverride`.

## Pendiente (no implementado aún)

- Editar y eliminar pacientes, citas, tratamientos y dentistas (hoy solo se puede cambiar el estado de una cita).
- Páginas MAUI para administrar dentistas y tratamientos (el API ya tiene sus controladores).
- Reportes (totales por día/mes, tratamientos más usados, citas por dentista).
- Antes de producción: quitar `DiagnosticoController`, poner las cadenas de UAT/Producción por variable de entorno y agregar autenticación (el API hoy es abierto: no pide login ni roles).

## Capturas de pantalla

TODO: agregar captura de la pantalla principal.

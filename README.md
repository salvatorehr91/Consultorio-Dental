# Consultorio Dental

Aplicación para un consultorio dental: app móvil y de escritorio en **.NET MAUI**, API REST en **ASP.NET Core 8** y base de datos en **SQL Server**.

> Estado: en desarrollo, funcional de extremo a extremo en local. Las pantallas crean, editan y consultan registros reales a través del API, con datos de ejemplo como respaldo si el API no responde. Validado contra `main` el 2026-11-19.

## Qué hace

- **Panel del día** con las citas de hoy, buscador de pacientes y resumen.
- **Pacientes**: lista con búsqueda, alta y edición con validación, eliminación (baja lógica) y acceso directo a su historial de pagos.
- **Citas**: lista con filtro por fecha y estado, alta y edición, cambio de estado, registro de pago desde la cita y eliminación (bloqueada si la cita ya tiene pagos).
- **Dentistas y tratamientos**: catálogos con alta, edición y baja lógica, con formularios que muestran todos los campos a la vez.
- **Pagos**: historial con filtros por fecha, paciente, método y estado, total de lo filtrado y comprobante por pago.
- **Comprobante**: folio `PAG-0000`, con los datos del consultorio, para ver en pantalla e imprimir desde el navegador.
- **Reportes**: total cobrado, cobros por día, tratamientos más usados y citas por dentista, con filtro por período (el mes en curso por defecto).

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
├── Maui-ClinicaDental/          # App MAUI (namespace Maui_ClinicaDental, páginas en .Pages)
│   ├── Pages/                   # Las 10 páginas (cada .xaml con su .xaml.cs)
│   │   ├── MainPage.xaml(.cs)       # Panel del día
│   │   ├── PacientesPage.xaml(.cs)  # Lista y búsqueda de pacientes
│   │   ├── NuevoPacientePage.xaml(.cs)
│   │   ├── CitasPage.xaml(.cs)      # Lista de citas con filtros
│   │   ├── NuevaCitaPage.xaml(.cs)  # Alta y edición de cita
│   │   ├── PagosPage.xaml(.cs)      # Historial de pagos y comprobante
│   │   ├── DentistasPage.xaml(.cs)  # Catálogo de dentistas
│   │   ├── NuevoDentistaPage.xaml(.cs)
│   │   ├── TratamientosPage.xaml(.cs) # Catálogo de tratamientos
│   │   ├── NuevoTratamientoPage.xaml(.cs)
│   │   └── ReportesPage.xaml(.cs)   # Reportes del consultorio
│   ├── AppShell.xaml(.cs)       # Rutas Shell (10 registradas)
│   ├── Models/                  # ApiModels.cs, Citas.cs, Pacientes.cs, …
│   └── Service/
│       └── ApiService.cs        # Consumo del API REST
└── ConsultorioDental.Api/       # API ASP.NET Core 8 (namespace ConsultorioDental.Api)
    ├── Program.cs               # Lee la cadena de DefaultConnection (variable de entorno primero)
    ├── appsettings.json         # DefaultConnection local + datos del consultorio
    ├── appsettings.Staging.json # UAT, sin cadena de conexión (va por variable de entorno)
    ├── appsettings.Production.json # Producción, sin cadena de conexión
    ├── Context/ConsultorioDbContext.cs
    ├── Models/Entidades.cs
    ├── Data/Dtos.cs
    └── Controllers/             # Pacientes, Dentistas, Tratamientos, Citas, Pagos, Reportes
```

Hay un README propio en cada proyecto: `Maui-ClinicaDental/README.md` y `ConsultorioDental.Api/README.md`.

## Base de datos

Ejecuta `DB/ConsultorioDental.sql` en SQL Server Management Studio o con `sqlcmd`. El script es idempotente: crea la base `ConsultorioDental` si no existe, crea las tablas solo si no existen y carga datos de ejemplo solo si las tablas están vacías.

Tablas: **Dentistas, Pacientes, Tratamientos, Citas, CitaTratamientos y Pagos**, con llaves foráneas e índices. Una cita tiene un paciente, un dentista, fecha y hora, duración, estado y puede incluir varios tratamientos; el precio queda guardado en `CitaTratamientos`, para que un cambio futuro en el catálogo no altere lo cobrado en citas pasadas.

Estados de cita: `Programada`, `Confirmada`, `Completada`, `Cancelada`, `NoAsistio`.
Métodos de pago: `Efectivo`, `Tarjeta`, `Transferencia`, `Otro`. Estados de pago: `Pagado`, `Pendiente`, `Reembolsado`.

Datos de ejemplo: dentistas Ana Pérez (General) y Luis Gómez (Ortodoncia); pacientes María López, Carlos Ruiz y Sofía Mendoza; tratamientos Limpieza dental ($800), Revisión general ($500) y Ortodoncia — revisión ($1200).

## API REST

Base: la URL del perfil de ejecución del API (en desarrollo, `https://localhost:65350`, según `launchSettings.json`; en UAT y Producción, `http://localhost:5000`). Swagger está activado en Development y Staging, en la raíz (`/`).

| Método | Ruta | Descripción |
| --- | --- | --- |
| GET | `/pacientes?buscar={texto}` | Lista de pacientes, con búsqueda |
| GET | `/pacientes/count` | Total de pacientes |
| GET | `/pacientes/{id}` | Un paciente |
| POST | `/pacientes` | Crear paciente |
| PUT | `/pacientes/{id}` | Editar paciente |
| DELETE | `/pacientes/{id}` | Baja lógica (`Activo = false`) |
| GET | `/dentistas` | Lista de dentistas |
| GET | `/dentistas/{id}` | Un dentista |
| POST | `/dentistas` | Crear dentista |
| PUT | `/dentistas/{id}` | Editar dentista |
| DELETE | `/dentistas/{id}` | Baja lógica |
| GET | `/tratamientos` | Lista de tratamientos |
| GET | `/tratamientos/{id}` | Un tratamiento |
| POST | `/tratamientos` | Crear tratamiento |
| PUT | `/tratamientos/{id}` | Editar tratamiento |
| DELETE | `/tratamientos/{id}` | Baja lógica |
| GET | `/citas?fecha={yyyy-MM-dd}&estado={estado}` | Lista de citas con filtros |
| GET | `/citas/hoy` | Citas de hoy (hora de México), ordenadas por hora |
| GET | `/citas/{id}` | Una cita |
| POST | `/citas` | Crear cita (inserta en `CitaTratamientos` al precio del catálogo) |
| PUT | `/citas/{id}` | Editar cita (si trae `TratamientoId`, reemplaza el tratamiento) |
| PUT | `/citas/{id}/estado` | Cambiar el estado de una cita |
| DELETE | `/citas/{id}` | Borrar cita; 409 si ya tiene pagos (mejor cancelarla) |
| GET | `/pagos?citaId=&pacienteId=&desde=&hasta=&metodo=&estado=` | Historial de pagos con filtros |
| GET | `/pagos/{id}` | Un pago |
| GET | `/pagos/{id}/comprobante` | Comprobante del pago (folio `PAG-0000`) |
| POST | `/pagos` | Registrar pago (valida cita, monto > 0, método y estado; la fecha la pone el servidor en UTC) |
| GET | `/reportes/resumen?desde=&hasta=` | Total cobrado, pagos, promedio, citas y citas por estado |
| GET | `/reportes/cobros-por-dia?desde=&hasta=` | Cobros por día (incluye días en $0.00) |
| GET | `/reportes/tratamientos-mas-usados?desde=&hasta=&top=10` | Tratamientos por veces realizados y total cobrado |
| GET | `/reportes/citas-por-dentista?desde=&hasta=` | Citas por dentista, con completadas y canceladas |

Decisiones deliberadas:

- **Registrar un pago y cambiar el estado de la cita son dos pasos separados.** Registrar el pago no marca la cita como `Completada`; el estado se cambia aparte.
- **El filtro de pagos y los reportes usan la hora del consultorio.** `FechaPago` se guarda en UTC, así que el servidor convierte el rango a hora de México y agrupa cada pago por su día en México; si no, un pago de la noche caería al día siguiente.
- **Solo los pagos con estado `Pagado` cuentan como cobrado** en los reportes. Pendiente no se cobró todavía y Reembolsado se devolvió.
- **Pacientes, dentistas y tratamientos se dan de baja lógica** (`Activo = false`) para no romper el historial de citas y pagos. Las citas se borran de verdad, salvo que ya tengan pagos.

## App MAUI

Páginas registradas en `AppShell` (las 10, en `Maui-ClinicaDental/Pages/`):

- **MainPage**: encabezado del consultorio, buscador de pacientes, resumen del día (citas de hoy y próxima cita), acciones rápidas y lista de citas de hoy.
- **PacientesPage**: lista con búsqueda y contador; al tocar un paciente se puede editar, eliminar o ir a **Ver pagos**, que abre el historial con ese paciente seleccionado y el filtro bloqueado.
- **NuevoPacientePage**: formulario con validación; crea o edita según venga con `?pacienteId=`.
- **CitasPage**: lista con filtro por fecha y estado; al tocar una cita ofrece **Ver detalle**, **Cambiar estado**, **Registrar pago** (monto y método, mostrando el total ya pagado de esa cita), **Ver pagos**, **Editar** y **Eliminar**.
- **NuevaCitaPage**: paciente, dentista, fecha, hora, tratamiento opcional, duración, estado, motivo y notas; valida paciente y dentista obligatorios y que la fecha/hora no sea pasada. Edita con `?citaId=`.
- **PagosPage**: historial con filtros por fecha (desde/hasta), paciente, método y estado; total de la lista filtrada; al tocar un pago muestra su comprobante, que se puede cerrar sin perder la lista. La lista es desplazable aunque el historial sea largo.
- **DentistasPage / TratamientosPage**: catálogos con alta, edición y baja lógica. Los formularios (**NuevoDentistaPage**, **NuevoTratamientoPage**) muestran todos los campos a la vez.
- **ReportesPage**: resumen del período, cobros por día, tratamientos más usados y citas por dentista; período por defecto, el mes en curso.

Si el API no responde, los `GET` devuelven `null` y la app muestra los datos de ejemplo con un aviso amarillo (`AvisoApi`), en vez de romperse. Los `POST`/`PUT`/`DELETE` devuelven `(false, motivo)`. En Reportes no hay datos de ejemplo: unos números inventados se confundirían con los reales.

La URL base del API está en `Service/ApiService.cs`: `https://localhost:65350/` fuera de Android y `http://10.0.2.2:5000/` en el emulador Android (el API debe escuchar en `http://0.0.0.0:5000`). Para un dispositivo físico, usa `ApiService.BaseUrlOverride` con la IP de tu PC y abre el puerto en el firewall.

## Configuración

### Cadena de conexión

`Program.cs` lee `ConnectionStrings:DefaultConnection` (con `SqlConnection` como respaldo del nombre viejo) y falla al arrancar con un mensaje claro si no hay ninguna. La cadena **no** está en `appsettings.Staging.json` ni en `appsettings.Production.json`: se pone por variable de entorno, que tiene prioridad sobre cualquier `appsettings.json`:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=TU_SERVIDOR;Database=ConsultorioDental;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True"
```

Para guardarla permanente en tu PC, hay un script en la documentación del proyecto (`configurar-conexion.ps1`): pregunta servidor, usuario y contraseña sin mostrarla y la guarda como variable de entorno de usuario, sin escribirla en ningún archivo. En desarrollo local también vale User Secrets:

```powershell
cd ConsultorioDental.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ConsultorioDental;Trusted_Connection=True;TrustServerCertificate=True"
```

En `appsettings.json` va solo la cadena base de desarrollo (localhost, `Trusted_Connection`, sin contraseña).

> Nunca subas contraseñas a Git. `appsettings.Development.json` no se sube al repo.

El entorno de pruebas se llama **Staging** en el proyecto (Swagger se enciende en Development y Staging); en la documentación es el **UAT**. Los perfiles de ejecución son Development (`https://localhost:65350`), UAT y Producción (`http://localhost:5000`).

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
2. **API**: abre `ConsultorioDental.Api/ConsultorioDental.Api.sln` en Visual Studio, revisa la cadena de conexión y corre el proyecto. Verifica en Swagger `GET /citas/hoy`.
3. **App MAUI**: abre la solución de `Maui-ClinicaDental`, ponla como proyecto de inicio y corre en Windows, Android o el emulador. Si el API corre en otro puerto o en otra máquina, ajusta `ApiService.BaseUrlOverride`.

## Pendiente (no implementado aún)

- **Login de administrador**: entregado 2026-11-20 en `login-admin-2026-11-20/` (pendiente de copiar y probar). El API ya no es abierto: cada llamada necesita el token JWT.
- **Bitácora** de cambios (quién creó, editó o eliminó cada registro). Ya se puede hacer, porque el login deja saber quién hizo cada cambio.
- Datos reales del consultorio en la sección `"Consultorio"` (hoy son marcadores de ejemplo).
- Antes de producción: agregar autenticación. El `DiagnosticoController` ya se quitó.
- Elegir dónde publicar el API y la base (Azure App Service + Azure SQL es la opción recomendada).

## Capturas de pantalla

TODO: agregar captura de la pantalla principal.

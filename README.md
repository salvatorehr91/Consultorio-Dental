# Consultorio Dental

Aplicación para un consultorio dental: app móvil y de escritorio en **.NET MAUI**, API REST en **ASP.NET Core 8** y base de datos en **SQL Server**.

> Estado: en desarrollo, funcional de extremo a extremo en local y en dispositivo físico Android (APK Ad Hoc). Las pantallas crean, editan y consultan registros reales a través del API, con datos de ejemplo como respaldo si el API no responde. Validado contra `main` (`03f0624`) el 2026-11-25.

## Qué hace

- **Panel del día** con las citas de hoy, buscador de pacientes y resumen.
- **Pacientes**: lista con búsqueda, alta y edición con validación, eliminación (baja lógica) y acceso directo a su historial de pagos.
- **Citas**: lista con filtro por fecha y estado, alta y edición, cambio de estado, registro de pago desde la cita y eliminación (bloqueada si la cita ya tiene pagos).
- **Dentistas y tratamientos**: catálogos con alta, edición y baja lógica, con formularios que muestran todos los campos a la vez.
- **Pagos**: historial con filtros por fecha, paciente, método y estado, total de lo filtrado y comprobante por pago.
- **Comprobante**: folio `PAG-0000`, con los datos del consultorio, para ver en pantalla e imprimir desde el navegador.
- **Reportes**: total cobrado, cobros por día, tratamientos más usados y citas por dentista, con filtro por período (el mes en curso por defecto).
- **Login de administrador**: inicio de sesión con JWT (roles `Admin` y `Recepcionista`); sin token el API devuelve 401 y los borrados exigen Admin.
- **Administrar usuarios**: alta de usuarios, restablecer contraseña y desactivar/reactivar (solo Admin; nunca se borran, para que la bitácora siempre sepa quién hizo cada cambio).
- **Menú AppShell**: acceso directo a Inicio, Pacientes, Citas, Pagos, Dentistas, Tratamientos, Reportes, Bitácora y Buscar actualización.
- **Bitácora**: registro de quién creó, editó o eliminó cada registro (solo Admin puede consultarla).
- **Indicador de carga**: cada pantalla muestra un indicador mientras valida el acceso al API; si el API no responde, el indicador desaparece y sale el aviso amarillo.
- **Actualización de la app**: al abrir, la app revisa si hay una versión nueva en el API y avisa con un botón Descargar (el APK se distribuye por GitHub Releases); también se puede revisar a mano desde el menú. Android no permite instalar en silencio fuera de Play Store: la instalación la confirma el usuario.
- **Aviso de cita nueva**: cuando se registra una cita nueva, el diente 🦷 de la pantalla principal se ilumina en dorado con el conteo (se revisa cada 2 minutos). Tocar el diente marca las citas como vistas y abre Citas.

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
│   └── ConsultorioDental.sql    # Base de datos: 8 tablas + datos de ejemplo
├── Maui-ClinicaDental/          # App MAUI (namespace Maui_ClinicaDental, páginas en .Pages)
│   ├── Pages/                   # Las 15 páginas (cada .xaml con su .xaml.cs)
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
│   │   ├── ReportesPage.xaml(.cs)   # Reportes del consultorio
│   │   ├── LoginPage.xaml(.cs)      # Inicio de sesión
│   │   ├── UsuariosPage.xaml(.cs)   # Administrar usuarios (solo Admin)
│   │   ├── NuevoUsuarioPage.xaml(.cs)
│   │   └── BitacoraPage.xaml(.cs)   # Bitácora de cambios (solo Admin)
│   ├── AppShell.xaml(.cs)       # Rutas Shell (15 registradas) + menú lateral
│   ├── Models/                  # ApiModels.cs, Citas.cs, Pacientes.cs, …
│   └── Service/
│       ├── ApiService.cs        # Consumo del API REST
│       ├── CargandoScope.cs     # Indicador de carga (se activa y se apaga solo)
│       ├── ActualizacionService.cs # Revisión de versión nueva (auto + manual)
│       └── AvisosCitas.cs       # Qué citas nuevas ya vio el usuario
└── ConsultorioDental.Api/       # API ASP.NET Core 8 (namespace ConsultorioDental.Api)
    ├── Program.cs               # Lee la cadena de DefaultConnection (variable de entorno primero)
    ├── appsettings.json         # DefaultConnection local + datos del consultorio + Actualizacion
    ├── appsettings.Staging.json # UAT, sin cadena de conexión (va por variable de entorno)
    ├── appsettings.Production.json # Producción, sin cadena de conexión
    ├── Context/ConsultorioDbContext.cs
    ├── Models/Entidades.cs
    ├── Data/Dtos.cs
    └── Controllers/             # Pacientes, Dentistas, Tratamientos, Citas, Pagos, Reportes, Auth, Bitacora, Version
```

Hay un README propio en cada proyecto: `Maui-ClinicaDental/README.md` y `ConsultorioDental.Api/README.md`.

## Base de datos

Ejecuta `DB/ConsultorioDental.sql` en SQL Server Management Studio o con `sqlcmd`. El script es idempotente: crea la base `ConsultorioDental` si no existe, crea las tablas solo si no existen y carga datos de ejemplo solo si las tablas están vacías.

Tablas: **Dentistas, Pacientes, Tratamientos, Citas, CitaTratamientos, Pagos, Usuarios y Bitacora**, con llaves foráneas e índices. Una cita tiene un paciente, un dentista, fecha y hora, duración, estado y puede incluir varios tratamientos; el precio queda guardado en `CitaTratamientos`, para que un cambio futuro en el catálogo no altere lo cobrado en citas pasadas. `Usuarios` guarda el login (hash de contraseña, rol `Admin`/`Recepcionista`); `Bitacora` guarda fecha, usuario, acción, entidad e id de cada cambio.

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
| GET | `/citas/nuevas?desdeId={id}` | Cuántas citas hay con id mayor que `desdeId` (aviso del diente) |
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
| POST | `/auth/login` | Iniciar sesión, devuelve el token JWT |
| POST | `/auth/cambiar-password` | Cambiar la contraseña del usuario logueado |
| POST | `/auth/usuarios` | Crear un usuario (solo Admin) |
| GET | `/auth/usuarios` | Lista de usuarios, sin el hash (solo Admin) |
| PUT | `/auth/usuarios/{id}/activo` | Desactivar/reactivar un usuario (solo Admin; no a uno mismo) |
| PUT | `/auth/usuarios/{id}/password` | Restablecer la contraseña de un usuario (solo Admin) |
| GET | `/bitacora?desde=&hasta=&usuarioId=&entidad=&accion=` | Bitácora de cambios, la más reciente primero (solo Admin) |
| GET | `/version` | Versión actual de la app y enlace del APK (sin login) |

Decisiones deliberadas:

- **Registrar un pago y cambiar el estado de la cita son dos pasos separados.** Registrar el pago no marca la cita como `Completada`; el estado se cambia aparte.
- **El filtro de pagos y los reportes usan la hora del consultorio.** `FechaPago` se guarda en UTC, así que el servidor convierte el rango a hora de México y agrupa cada pago por su día en México; si no, un pago de la noche caería al día siguiente.
- **Solo los pagos con estado `Pagado` cuentan como cobrado** en los reportes. Pendiente no se cobró todavía y Reembolsado se devolvió.
- **Pacientes, dentistas y tratamientos se dan de baja lógica** (`Activo = false`) para no romper el historial de citas y pagos. Las citas se borran de verdad, salvo que ya tengan pagos.

## App MAUI

Páginas registradas en `AppShell` (las 15, en `Maui-ClinicaDental/Pages/`). El menú lateral da acceso directo a Inicio, Pacientes, Citas, Pagos, Dentistas, Tratamientos, Reportes, Bitácora y Buscar actualización; las páginas de formulario y el Login no están en el menú:

- **MainPage**: encabezado del consultorio, buscador de pacientes, resumen del día (citas de hoy y próxima cita), acciones rápidas y lista de citas de hoy.
- **PacientesPage**: lista con búsqueda y contador; al tocar un paciente se puede editar, eliminar o ir a **Ver pagos**, que abre el historial con ese paciente seleccionado y el filtro bloqueado.
- **NuevoPacientePage**: formulario con validación; crea o edita según venga con `?pacienteId=`.
- **CitasPage**: lista con filtro por fecha y estado; al tocar una cita ofrece **Ver detalle**, **Cambiar estado**, **Registrar pago** (monto y método, mostrando el total ya pagado de esa cita), **Ver pagos**, **Editar** y **Eliminar**.
- **NuevaCitaPage**: paciente, dentista, fecha, hora, tratamiento opcional, duración, estado, motivo y notas; valida paciente y dentista obligatorios y que la fecha/hora no sea pasada. Edita con `?citaId=`.
- **PagosPage**: historial con filtros por fecha (desde/hasta), paciente, método y estado; total de la lista filtrada; al tocar un pago muestra su comprobante, que se puede cerrar sin perder la lista. La lista es desplazable aunque el historial sea largo.
- **DentistasPage / TratamientosPage**: catálogos con alta, edición y baja lógica. Los formularios (**NuevoDentistaPage**, **NuevoTratamientoPage**) muestran todos los campos a la vez.
- **ReportesPage**: resumen del período, cobros por día, tratamientos más usados y citas por dentista; período por defecto, el mes en curso.
- **LoginPage**: inicio de sesión; el token se guarda en `SecureStorage` y la sesión en `Preferences`. Sin sesión, la app abre el Login en vez del menú.
- **UsuariosPage / NuevoUsuarioPage**: lista de usuarios con activar/desactivar y restablecer contraseña, y formulario de alta (usuario, contraseña mínimo 8 caracteres, rol). Solo Admin (el servidor también lo bloquea con 403).
- **BitacoraPage**: bitácora con filtros por período y entidad; la hora se muestra en hora local. Solo Admin.

**Indicador de carga**: cada pantalla muestra un `ActivityIndicator` mientras valida el acceso al API (`Service/CargandoScope.cs`: una línea por método, se apaga solo en cualquier salida). Mientras carga, las listas se ocultan para que no parpadee el "No hay registros", y los botones Entrar/Guardar se bloquean.

**Actualización**: al abrir MainPage la app consulta `GET /version` una vez por sesión; si hay una versión más reciente, avisa con Descargar (abre el enlace del APK en el navegador) o Después. El menú "Buscar actualización" revisa a mano y dice "Estás al día" si no hay nada nuevo. La versión instalada es `ApplicationDisplayVersion` del `.csproj` (`com.consultoriodental.app`, versión `1.0`).

**Aviso de cita nueva**: MainPage revisa cada 2 minutos `GET /citas/nuevas` y, si hay citas con id mayor que la última vista, el diente 🦷 se ilumina en dorado con el conteo. Tocar el diente (o abrir Citas) las marca como vistas. La primera vez no avisa de lo que ya existe, y una cita creada desde el mismo teléfono no enciende el aviso. El aviso solo funciona con la app abierta; con la app cerrada haría falta notificación push (no implementada).

Si el API no responde, los `GET` devuelven `null` y la app muestra los datos de ejemplo con un aviso amarillo (`AvisoApi`), en vez de romperse. Los `POST`/`PUT`/`DELETE` devuelven `(false, motivo)`. En Reportes no hay datos de ejemplo: unos números inventados se confundirían con los reales.

La URL base del API está en `Service/ApiService.cs`: `https://localhost:65350/` fuera de Android y `http://10.0.2.2:5000/` en el emulador Android (el API debe escuchar en `http://0.0.0.0:5000`). Para un dispositivo físico, usa `ApiService.BaseUrlOverride` con la IP de tu PC y abre el puerto en el firewall (o expón el API con el reenvío de puertos de VS Code y pon esa URL pública).

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

### Actualización de la app (APK)

En `appsettings.json`, sección `"Actualizacion"`:

```json
"Actualizacion": {
  "Version": "1.0.0",
  "UrlApk": "https://github.com/salvatorehr91/Consultorio-Dental/releases/latest/download/consultorio-dental.apk",
  "Notas": ""
}
```

`Version` debe ser la misma que `ApplicationDisplayVersion` en el `.csproj` de la app. `UrlApk` es la descarga directa del APK publicado en GitHub Releases (sube el APK firmado como archivo del release, con el nombre `consultorio-dental.apk`; el enlace `releases/latest/download/...` siempre apunta al APK de la última versión). Para publicar una versión nueva: sube `ApplicationDisplayVersion`, genera el APK (Publish → Ad Hoc, el firmado `-Signed.apk`), súbelo a un release nuevo y pon la misma versión en `appsettings.json`. El APK y el AAB **no** se suben al repo (están en el `.gitignore` como `*.apk` y `*.aab`); el `.keystore` de la firma tampoco se sube nunca.

## Cómo correrlo

1. **Base de datos**: ejecuta `DB/ConsultorioDental.sql` en SQL Server.
2. **API**: abre `ConsultorioDental.Api/ConsultorioDental.Api.sln` en Visual Studio, revisa la cadena de conexión y corre el proyecto. Verifica en Swagger `GET /citas/hoy` (Swagger tiene botón Authorize: inicia sesión en `POST /auth/login` y pega el token).
3. **App MAUI**: abre la solución de `Maui-ClinicaDental`, ponla como proyecto de inicio y corre en Windows, Android o el emulador. Si el API corre en otro puerto o en otra máquina, ajusta `ApiService.BaseUrlOverride`.
4. **En un teléfono físico**: genera el APK (Publish → Ad Hoc, el archivo `-Signed.apk`), instálalo y apunta la app al API por la IP de tu PC o por la URL pública del reenvío de VS Code.

## Pendiente (no implementado aún)

- Datos reales del consultorio en la sección `"Consultorio"` (hoy son marcadores de ejemplo).
- Captura de la pantalla principal (para este README).
- Elegir dónde publicar el API y la base (Azure App Service + Azure SQL es la opción recomendada); hoy el API corre en local y el teléfono llega por el reenvío de VS Code.
- Decidir la limpieza del secreto viejo de GitHub: el `main` está limpio, pero los commits viejos siguen accesibles por el PR #1 (solo GitHub Support puede borrar ese registro) o hacer el repo privado.

## Capturas de pantalla

TODO: agregar captura de la pantalla principal.

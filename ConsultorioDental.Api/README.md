# Consultorio Dental — API REST (ASP.NET Core 8)

API REST para el consultorio dental: pacientes, dentistas, tratamientos, citas, pagos con comprobante y reportes. La base de datos es SQL Server (script en `../DB/ConsultorioDental.sql`); Entity Framework Core solo la lee, no la crea.

> Estado: funcional de extremo a extremo en local. Validado contra `main` (`03f0624`) el 2026-11-25.

## Tecnologías

- ASP.NET Core 8 Web API, Entity Framework Core, Swagger (en Development y Staging, en la raíz `/`).
- SQL Server. Sin migraciones: las tablas las crea `DB/ConsultorioDental.sql`.
- JSON en camelCase por defecto; los DTOs están en `Data/Dtos.cs`.

## Estructura

```
ConsultorioDental.Api/
├── Program.cs                        # Configuración: cadena de conexión, DbContext, Swagger
├── appsettings.json                  # DefaultConnection local + datos del consultorio
├── appsettings.Staging.json          # UAT, sin cadena (va por variable de entorno)
├── appsettings.Production.json       # Producción, sin cadena (va por variable de entorno)
├── Context/
│   └── ConsultorioDbContext.cs       # DbSets y relaciones (namespace ConsultorioDental.Api.Context)
├── Models/
│   └── Entidades.cs                  # Dentista, Paciente, Tratamiento, Cita, CitaTratamiento, Pago, Usuario, Bitacora
├── Data/
│   ├── Dtos.cs                       # Contratos JSON (DTOs de lectura, creación, edición y reportes)
│   └── BitacoraService.cs            # Registra cambios en la bitácora (nunca rompe la operación)
└── Controllers/
    ├── PacientesController.cs
    ├── DentistasController.cs
    ├── TratamientosController.cs
    ├── CitasController.cs            # Incluye GET /hoy, GET /nuevas y PUT /{id}/estado
    ├── PagosController.cs            # Incluye GET /{id}/comprobante
    ├── ReportesController.cs
    ├── AuthController.cs             # Login JWT y administración de usuarios
    ├── BitacoraController.cs         # GET /bitacora (solo Admin)
    └── VersionController.cs          # GET /version (sin login)
```

## Base de datos

Ejecuta `../DB/ConsultorioDental.sql` en SQL Server. Crea la base `ConsultorioDental` si no existe y las 8 tablas: **Dentistas, Pacientes, Tratamientos, Citas, CitaTratamientos, Pagos, Usuarios y Bitacora**.

Estados de cita: `Programada`, `Confirmada`, `Completada`, `Cancelada`, `NoAsistio`.
Métodos de pago: `Efectivo`, `Tarjeta`, `Transferencia`, `Otro`. Estados de pago: `Pagado`, `Pendiente`, `Reembolsado`.

## Endpoints

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
| DELETE | `/citas/{id}` | Borrar cita; 409 si ya tiene pagos |
| GET | `/pagos?citaId=&pacienteId=&desde=&hasta=&metodo=&estado=` | Historial de pagos con filtros |
| GET | `/pagos/{id}` | Un pago |
| GET | `/pagos/{id}/comprobante` | Comprobante del pago (folio `PAG-0000`) |
| POST | `/pagos` | Registrar pago (valida cita, monto > 0, método y estado; la fecha la pone el servidor en UTC) |
| GET | `/reportes/resumen?desde=&hasta=` | Total cobrado, pagos, promedio, citas y citas por estado |
| GET | `/reportes/cobros-por-dia?desde=&hasta=` | Cobros por día (incluye días en $0.00) |
| GET | `/reportes/tratamientos-mas-usados?desde=&hasta=&top=10` | Tratamientos por veces realizados y total cobrado |
| GET | `/reportes/citas-por-dentista?desde=&hasta=` | Citas por dentista, con completadas y canceladas |

Sin `desde`/`hasta`, los reportes usan el mes en curso (hora de México). El período se limita a 366 días.

| Método | Ruta | Descripción |
| --- | --- | --- |
| POST | `/auth/login` | Iniciar sesión, devuelve el token JWT |
| POST | `/auth/cambiar-password` | Cambiar la contraseña del usuario logueado |
| POST | `/auth/usuarios` | Crear un usuario (solo Admin) |
| GET | `/auth/usuarios` | Lista de usuarios, sin el hash (solo Admin) |
| PUT | `/auth/usuarios/{id}/activo` | Desactivar/reactivar un usuario (solo Admin; no a uno mismo) |
| PUT | `/auth/usuarios/{id}/password` | Restablecer la contraseña de un usuario (solo Admin) |
| GET | `/bitacora?desde=&hasta=&usuarioId=&entidad=&accion=` | Bitácora de cambios, la más reciente primero (solo Admin) |
| GET | `/version` | Versión actual de la app y enlace del APK (sin login) |

## Decisiones que se tomaron

- **Baja lógica vs borrado real.** Pacientes, dentistas y tratamientos se desactivan (`Activo = false`) para no romper el historial de citas y pagos. Las citas se borran de verdad, pero si ya tienen pagos el servidor devuelve 409 y sugiere cancelarla.
- **Pago y estado son dos pasos separados.** `POST /pagos` no cambia el estado de la cita; eso se hace con `PUT /citas/{id}/estado`.
- **Fechas: citas en hora local, pagos en UTC.** `FechaHora` de la cita se guarda como llega (hora del consultorio). `FechaPago` se pone en el servidor con `DateTime.UtcNow`. Por eso los filtros de pagos y los reportes convierten el rango a hora de México: un pago de las 20:00 en CDMX no debe caer al día siguiente. `GET /citas/hoy` también usa la hora de México, no `DateTime.Today` del servidor.
- **Solo `Pagado` cuenta como cobrado** en los reportes. El total por tratamiento es lo cobrado en su momento (`Precio × Cantidad` de `CitaTratamientos`), no el precio actual del catálogo.
- **El comprobante trae los datos del consultorio** desde la sección `"Consultorio"` de `appsettings.json` (nombre, dirección, teléfono, email). El folio es `PAG-{PagoId:0000}`.
- **Autenticación con JWT.** Cada llamada (salvo el login y `GET /version`) necesita el token como `Authorization: Bearer <token>`. `POST /auth/login` lo devuelve. Los DELETE solo los puede hacer un usuario con rol `Admin`. Swagger tiene botón Authorize: se inicia sesión en `POST /auth/login`, se copia el token y se pega (Swagger agrega el "Bearer").
- **Bitácora.** Cada crear/editar/eliminar exitoso registra quién (del JWT), qué acción, qué entidad e id y un detalle. El registro se hace después de guardar y, si falla, nunca rompe la operación. Los logins y los GET no se registran. Los usuarios nunca se borran, solo se desactivan, para que la bitácora siempre pueda decir quién hizo cada cambio.
- **Reportes: agrupar en memoria.** EF Core no traduce el `GroupBy` del servidor en este proyecto, así que los reportes traen una proyección plana con `ToListAsync()` y agrupan en memoria (LINQ-to-Objects). El volumen es pequeño (período máximo 366 días).
- **`GET /version` es público a propósito.** La app lo consulta al abrir y con la sesión vencida; solo expone la versión disponible y la URL pública del APK.

## Configuración

### Cadena de conexión

`Program.cs` busca `ConnectionStrings:DefaultConnection` (con `SqlConnection` como respaldo del nombre viejo) y falla al arrancar con un mensaje claro si no hay ninguna. La cadena **no** está en `appsettings.Staging.json` ni en `appsettings.Production.json`: se pone por variable de entorno, que tiene prioridad sobre cualquier `appsettings.json`:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=TU_SERVIDOR;Database=ConsultorioDental;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True"
```

En desarrollo local también vale User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ConsultorioDental;Trusted_Connection=True;TrustServerCertificate=True"
```

En `appsettings.json` va solo la cadena base de desarrollo (localhost, `Trusted_Connection`, sin contraseña).

> Nunca subas contraseñas a Git. `appsettings.Development.json` no se sube al repo.

### Entornos

El entorno de pruebas se llama **Staging** en el proyecto (Swagger se enciende en Development y Staging); en la documentación es el **UAT**. Perfiles de ejecución en `launchSettings.json`:

| Perfil | Entorno | URL |
| --- | --- | --- |
| ConsultorioDental.Api | Development | `https://localhost:65350;http://localhost:65351` |
| UAT | Staging | `http://localhost:5000` |
| Producción | Production | `http://localhost:5000` |

El puerto 5000 en HTTP es el que espera el emulador Android (`http://10.0.2.2:5000/`): para UAT local el API debe escuchar en `http://0.0.0.0:5000`.

### Datos del consultorio (comprobante)

En `appsettings.json`, sección `"Consultorio"` (hoy con valores de ejemplo; cambia los por los reales):

```json
"Consultorio": {
  "Nombre": "Consultorio Dental",
  "Direccion": "Domicilio del Consultorio",
  "Telefono": "55 5555 5555",
  "Email": "email@consultorio.com"
}
```

### Actualización de la app (APK)

En `appsettings.json`, sección `"Actualizacion"`:

```json
"Actualizacion": {
  "Version": "1.0.0",
  "UrlApk": "https://github.com/salvatorehr91/Consultorio-Dental/releases/latest/download/consultorio-dental.apk",
  "Notas": ""
}
```

`Version` debe ser la misma que `ApplicationDisplayVersion` en el `.csproj` de la app. `UrlApk` es la descarga directa del APK publicado en GitHub Releases (el archivo del release, con el nombre `consultorio-dental.apk`).

## Cómo correrlo

1. Ejecuta `../DB/ConsultorioDental.sql` en SQL Server.
2. Abre `ConsultorioDental.Api.sln` en Visual Studio, revisa la cadena de conexión (o pon la variable de entorno).
3. Corre el proyecto y verifica en Swagger (raíz `/`): `GET /citas/hoy`.

## Pendiente (de este proyecto)

- Publicar UAT/Producción (Azure App Service + Azure SQL recomendado) con la cadena por variable de entorno del servicio.

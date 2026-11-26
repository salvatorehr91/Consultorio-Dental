# Consultorio Dental — App .NET MAUI

App móvil y de escritorio para un consultorio dental: panel del día, pacientes, citas, dentistas, tratamientos, pagos con comprobante, reportes, login con roles, administración de usuarios y bitácora. Consume el API REST de `ConsultorioDental.Api` y usa SQL Server a través de ese API (la app nunca se conecta directo a la base).

> Estado: funcional de extremo a extremo en local y en dispositivo físico Android (APK Ad Hoc, paquete `com.consultoriodental.app`). Validado contra `main` (`03f0624`) el 2026-11-25.

## Tecnologías

- .NET MAUI 10 (Android, iOS, MacCatalyst, Windows), C# / XAML, Shell.
- Un solo `HttpClient` en `Service/ApiService.cs`; JSON en camelCase.
- Sin base local: todos los datos vienen del API; si no responde, la app muestra datos de ejemplo con un aviso amarillo (`AvisoApi`), en vez de romperse (salvo en Reportes, donde no hay datos de ejemplo).

## Estructura

```
Maui-ClinicaDental/
├── Pages/                        # Las 15 páginas (namespace Maui_ClinicaDental.Pages)
│   ├── MainPage.xaml(.cs)        # Panel del día: citas de hoy, buscador, resumen
│   ├── PacientesPage.xaml(.cs)   # Lista con búsqueda; editar, eliminar, ver pagos
│   ├── NuevoPacientePage.xaml(.cs) # Formulario crear/editar (?pacienteId=)
│   ├── CitasPage.xaml(.cs)       # Lista con filtro por fecha y estado
│   ├── NuevaCitaPage.xaml(.cs)   # Formulario crear/editar (?citaId=)
│   ├── PagosPage.xaml(.cs)       # Historial de pagos, filtros y comprobante
│   ├── DentistasPage.xaml(.cs)   # Catálogo de dentistas
│   ├── NuevoDentistaPage.xaml(.cs) # Formulario crear/editar (?dentistaId=)
│   ├── TratamientosPage.xaml(.cs) # Catálogo de tratamientos
│   ├── NuevoTratamientoPage.xaml(.cs) # Formulario crear/editar (?tratamientoId=)
│   ├── ReportesPage.xaml(.cs)    # Resumen, cobros por día, tratamientos, dentistas
│   ├── LoginPage.xaml(.cs)       # Inicio de sesión
│   ├── UsuariosPage.xaml(.cs)    # Administrar usuarios (solo Admin)
│   ├── NuevoUsuarioPage.xaml(.cs) # Formulario crear usuario
│   └── BitacoraPage.xaml(.cs)    # Bitácora de cambios (solo Admin)
├── AppShell.xaml(.cs)            # Shell + 15 rutas registradas + menú lateral
├── App.xaml(.cs)                 # Arranque (MainPage = new AppShell() o LoginPage si no hay sesión)
├── MauiProgram.cs
├── Models/
│   ├── ApiModels.cs              # DTOs del API (con [JsonPropertyName] camelCase)
│   ├── Citas.cs / Pacientes.cs   # Modelos de vista (CitaItem, PacienteItem…)
│   ├── Dentistas.cs / Tratamientos.cs
│   └── CitasHoy.cs
└── Service/
    ├── ApiService.cs             # GET/POST/PUT/DELETE contra el API
    ├── CargandoScope.cs          # Indicador de carga (se activa y se apaga solo)
    ├── ActualizacionService.cs   # Revisión de versión nueva (auto + manual)
    └── AvisosCitas.cs            # Qué citas nuevas ya vio el usuario
```

## Páginas y rutas

| Página | Ruta Shell | Qué hace |
| --- | --- | --- |
| MainPage | `MainPage` (inicio) | Citas de hoy, buscador, resumen, acciones rápidas |
| PacientesPage | `PacientesPage` (+ `?buscar=`) | Lista, búsqueda, editar, eliminar, ver pagos del paciente |
| NuevoPacientePage | `NuevoPacientePage` (+ `?pacienteId=`) | Formulario crear/editar paciente |
| CitasPage | `CitasPage` | Lista con filtros; detalle, cambiar estado, registrar pago, ver pagos, editar, eliminar |
| NuevaCitaPage | `NuevaCitaPage` (+ `?citaId=`) | Formulario crear/editar cita |
| PagosPage | `PagosPage` (+ `?citaId=&pacienteId=&pacienteNombre=`) | Historial con filtros, total filtrado y comprobante |
| DentistasPage | `DentistasPage` | Catálogo; alta, edición y baja lógica |
| NuevoDentistaPage | `NuevoDentistaPage` (+ `?dentistaId=`) | Formulario crear/editar dentista |
| TratamientosPage | `TratamientosPage` | Catálogo; alta, edición y baja lógica |
| NuevoTratamientoPage | `NuevoTratamientoPage` (+ `?tratamientoId=`) | Formulario crear/editar tratamiento |
| ReportesPage | `ReportesPage` | Resumen, cobros por día, tratamientos más usados, citas por dentista |
| LoginPage | `LoginPage` | Inicio de sesión (usuario y contraseña; el token va a `SecureStorage`) |
| UsuariosPage | `UsuariosPage` | Lista de usuarios; restablecer contraseña, desactivar/reactivar (solo Admin) |
| NuevoUsuarioPage | `NuevoUsuarioPage` | Formulario crear usuario (usuario, contraseña, rol) |
| BitacoraPage | `BitacoraPage` | Bitácora con filtros por período y entidad (solo Admin) |

## Decisiones que se tomaron

- **Formularios, no preguntas una por una.** Crear y editar se hacen en una página con todos los campos a la vez (como `NuevoPacientePage`). Las preguntas en pantalla una por una se quitaron porque, si se cancelaba alguna, no se guardaba nada y parecía que no actualizaba.
- **Las listas se desplazan.** La raíz de las páginas de listas es un `Grid` con la lista en una fila `*`, no un `VerticalStackLayout`: con el stack, el `CollectionView` tomaba altura infinita y la página nunca se desplazaba.
- **Editar reutiliza el formulario de alta.** La misma página crea (sin parámetro) o edita (con `?id=`, precargando con `GET /{entidad}/{id}`).
- **Eliminar pacientes, dentistas y tratamientos es baja lógica** (`Activo = false` en el servidor) para no romper el historial. Eliminar una cita es borrado real, bloqueado con 409 si ya tiene pagos.
- **Registrar un pago no cambia el estado de la cita.** Son dos pasos separados: el pago se registra y el estado se cambia aparte.
- **Fechas en hora local.** `FechaPago` se guarda en UTC en el servidor; la app filtra y muestra en hora local (por eso un pago de la noche no cae al día siguiente).
- **Comprobante sin API de impresión.** MAUI no tiene impresión multiplataforma: el comprobante se genera como HTML y se abre en el navegador (Ctrl+P) o se comparte por la hoja del sistema.
- **Login con sesión guardada.** El token JWT va a `SecureStorage` y la sesión a `Preferences`, así `App.xaml.cs` puede elegir entre AppShell y LoginPage al arrancar. Un 401 limpia la sesión ("Sesión expirada") y un 403 avisa que se requiere Admin. Eliminar solo aparece en los menús si el usuario es Admin.
- **Indicador de carga en todas las pantallas.** Cada método que llama al API usa `CargandoScope.Activar(...)` (una línea): el indicador se enciende al entrar y se apaga en cualquier salida, así nunca se queda trabado. Mientras carga, las listas se ocultan para que no parpadee el "No hay registros", y Entrar/Guardar se bloquean.
- **Actualización por APK.** La app se distribuye por APK (Ad Hoc), no por Play Store, así que Android no instala en silencio: la app revisa sola `GET /version` al abrir (una vez por sesión) y avisa con Descargar, y el menú "Buscar actualización" revisa a mano. La versión instalada es `ApplicationDisplayVersion` del `.csproj`.
- **El diente avisa de citas nuevas.** MainPage revisa cada 2 minutos `GET /citas/nuevas` (conteo e id mayor). Si hay citas con id mayor que la última vista, el diente 🦷 se ilumina en dorado con el conteo. Tocar el diente (o abrir Citas) las marca como vistas. La primera vez no avisa de lo que ya existe, y una cita creada desde el mismo teléfono no enciende el aviso. Solo funciona con la app abierta; con la app cerrada haría falta notificación push.
- **El menú navega igual que los botones.** Los MenuItems de AppShell usan navegación relativa (`GoToAsync("CitasPage")`), igual que los botones de MainPage, y cierran el menú antes de navegar (en Android, navegar con el menú abierto cerraba la app).

## Configuración del API

La URL base está en `Service/ApiService.cs`:

| Dónde corre la app | URL base |
| --- | --- |
| Windows / macOS / simulador iOS | `https://localhost:65350/` |
| Emulador Android | `http://10.0.2.2:5000/` (el API debe escuchar en `http://0.0.0.0:5000`) |
| Dispositivo físico | `ApiService.BaseUrlOverride` con la IP de tu PC + puerto 5000 abierto en el firewall |

En desarrollo se usa HTTP en Android para no pelear con el certificado autofirmado de HTTPS. En producción, cambia a `https://tu-dominio/`.

## Publicar el APK (Android)

La app es .NET 10 (`net10.0-android`): compila con el SDK de .NET 10 (el SDK 9 no sirve). Paquete `com.consultoriodental.app` (`ApplicationId` en el `.csproj`).

1. En Visual Studio: clic derecho en el proyecto → Publish → **Ad Hoc** (el AAB es solo para Play Store, no se instala directo) → crea o elige la clave de firma (`.keystore`, guarda su contraseña: sin ella no se puede firmar una actualización).
2. El APK firmado (`-Signed.apk`) queda en `bin\Release\net10.0-android\publish\`. El APK sin `-Signed` no está firmado y no se instala. Renombrarlo no afecta la firma: se puede llamar `consultorio-dental.apk`.
3. Súbelo como archivo a un **Release de GitHub** (no al repo: `*.apk` y `*.aab` están en el `.gitignore`). La URL `releases/latest/download/consultorio-dental.apk` siempre apunta al APK de la última versión: es la que va en `appsettings.json` → `"Actualizacion:UrlApk"`.
4. Para publicar una versión nueva: sube `ApplicationDisplayVersion` en el `.csproj`, genera el APK, súbelo a un release nuevo y pon la misma versión en `"Actualizacion:Version"` del API.

Si en el teléfono ya estaba la app instalada desde Visual Studio (clave de depuración), el APK firmado no se instala encima (las firmas no coinciden): desinstala la app primero.

## Cómo correrlo

1. Levanta el API (ver `../ConsultorioDental.Api/README.md`) y verifica en Swagger `GET /citas/hoy`.
2. Abre la solución de `Maui-ClinicaDental` en Visual Studio, ponla como proyecto de inicio.
3. Corre en Windows o en el emulador Android. Para probar sin el API, la app muestra los datos de ejemplo con el aviso amarillo.
4. En un teléfono físico: instala el APK firmado y apunta la app al API por la IP de tu PC o por la URL pública del reenvío de puertos de VS Code.

## Pendiente (de este proyecto)

- Datos reales del consultorio en el comprobante (se configuran en el API, sección `"Consultorio"` de `appsettings.json`).

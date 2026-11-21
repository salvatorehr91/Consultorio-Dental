# Consultorio Dental — App .NET MAUI

App móvil y de escritorio para un consultorio dental: panel del día, pacientes, citas, dentistas, tratamientos, pagos con comprobante y reportes. Consume el API REST de `ConsultorioDental.Api` y usa SQL Server a través de ese API (la app nunca se conecta directo a la base).

> Estado: funcional de extremo a extremo en local. Validado contra `main` el 2026-11-19.

## Tecnologías

- .NET MAUI 10 (Android, iOS, MacCatalyst, Windows), C# / XAML, Shell.
- Un solo `HttpClient` en `Service/ApiService.cs`; JSON en camelCase.
- Sin base local: todos los datos vienen del API; si no responde, la app muestra datos de ejemplo con un aviso amarillo (`AvisoApi`), en vez de romperse (salvo en Reportes, donde no hay datos de ejemplo).

## Estructura

```
Maui-ClinicaDental/
├── Pages/                        # Las 10 páginas (namespace Maui_ClinicaDental.Pages)
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
│   └── ReportesPage.xaml(.cs)    # Resumen, cobros por día, tratamientos, dentistas
├── AppShell.xaml(.cs)            # Shell + 10 rutas registradas
├── App.xaml(.cs)                 # Arranque (MainPage = new AppShell())
├── MauiProgram.cs
├── Models/
│   ├── ApiModels.cs              # DTOs del API (con [JsonPropertyName] camelCase)
│   ├── Citas.cs / Pacientes.cs   # Modelos de vista (CitaItem, PacienteItem…)
│   ├── Dentistas.cs / Tratamientos.cs
│   └── CitasHoy.cs
└── Service/
    └── ApiService.cs             # GET/POST/PUT/DELETE contra el API
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

## Decisiones que se tomaron

- **Formularios, no preguntas una por una.** Crear y editar se hacen en una página con todos los campos a la vez (como `NuevoPacientePage`). Las preguntas en pantalla una por una se quitaron porque, si se cancelaba alguna, no se guardaba nada y parecía que no actualizaba.
- **Las listas se desplazan.** La raíz de las páginas de listas es un `Grid` con la lista en una fila `*`, no un `VerticalStackLayout`: con el stack, el `CollectionView` tomaba altura infinita y la página nunca se desplazaba.
- **Editar reutiliza el formulario de alta.** La misma página crea (sin parámetro) o edita (con `?id=`, precargando con `GET /{entidad}/{id}`).
- **Eliminar pacientes, dentistas y tratamientos es baja lógica** (`Activo = false` en el servidor) para no romper el historial. Eliminar una cita es borrado real, bloqueado con 409 si ya tiene pagos.
- **Registrar un pago no cambia el estado de la cita.** Son dos pasos separados: el pago se registra y el estado se cambia aparte.
- **Fechas en hora local.** `FechaPago` se guarda en UTC en el servidor; la app filtra y muestra en hora local (por eso un pago de la noche no cae al día siguiente).
- **Comprobante sin API de impresión.** MAUI no tiene impresión multiplataforma: el comprobante se genera como HTML y se abre en el navegador (Ctrl+P) o se comparte por la hoja del sistema.

## Configuración del API

La URL base está en `Service/ApiService.cs`:

| Dónde corre la app | URL base |
| --- | --- |
| Windows / macOS / simulador iOS | `https://localhost:65350/` |
| Emulador Android | `http://10.0.2.2:5000/` (el API debe escuchar en `http://0.0.0.0:5000`) |
| Dispositivo físico | `ApiService.BaseUrlOverride` con la IP de tu PC + puerto 5000 abierto en el firewall |

En desarrollo se usa HTTP en Android para no pelear con el certificado autofirmado de HTTPS. En producción, cambia a `https://tu-dominio/`.

## Cómo correrlo

1. Levanta el API (ver `../ConsultorioDental.Api/README.md`) y verifica en Swagger `GET /citas/hoy`.
2. Abre la solución de `Maui-ClinicaDental` en Visual Studio, ponla como proyecto de inicio.
3. Corre en Windows o en el emulador Android. Para probar sin el API, la app muestra los datos de ejemplo con el aviso amarillo.

## Pendiente (de este proyecto)

- Login de administrador: entregado 2026-11-20 en `login-admin-2026-11-20/` (pendiente de copiar y probar).
- Datos reales del consultorio en el comprobante (se configuran en el API, sección `"Consultorio"` de `appsettings.json`).

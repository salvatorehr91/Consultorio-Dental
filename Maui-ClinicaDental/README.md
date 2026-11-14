# Consultorio Dental — .NET MAUI, primer hito

## Qué hay aquí

- **MainPage.xaml** — Layout de la página principal: encabezado, buscador, resumen del día, acciones rápidas y lista de citas de hoy.
- **MainPage.xaml.cs** — Code-behind con datos de ejemplo y un ejemplo comentado de consumo REST con `HttpClient`.
- **ConsultorioDental.sql** — Script para SQL Server con las tablas principales y datos de ejemplo.

## Suposiciones que tomé

No me dijiste qué página ni qué entidades eran las principales, así que partí de lo más común en un consultorio:

- La página principal es un panel del día, no un formulario.
- Las tablas principales son: **Dentistas, Pacientes, Tratamientos, Citas, CitaTratamientos y Pagos**.
- Una cita tiene un paciente, un dentista, una fecha/hora, un estado y puede incluir varios tratamientos.
- Los precios se guardan también en `CitaTratamientos`, para que un cambio futuro en el catálogo no altere lo cobrado en citas pasadas.

Si tu consultorio maneja otras entidades (por ejemplo, historial clínico, recetas o seguros), dime cuáles y las agrego.

## Cómo usarlo

### 1. Base de datos
Ejecuta `ConsultorioDental.sql` en SQL Server Management Studio o con `sqlcmd`. Crea la base `ConsultorioDental` si no existe, crea las tablas solo si no existen, y carga datos de ejemplo solo si las tablas están vacías.

### 2. App MAUI
1. Copia `MainPage.xaml` y `MainPage.xaml.cs` a tu proyecto MAUI, en el espacio de nombres `ConsultorioDental` (o cambia el `x:Class` y el namespace al tuyo).
2. Registra la página en `AppShell.xaml` si usas Shell, con las rutas `NuevaCitaPage`, `NuevoPacientePage`, `PacientesPage` y `CitasPage`, o cambia esos nombres en el code-behind.
3. Cambia `ApiBaseUrl` en `MainPage.xaml.cs` por la URL de tu API REST.

### 3. API REST sugerida
Estas rutas encajan con las tablas y con el layout:

- `GET /api/citas/hoy` — citas de hoy para la página principal
- `GET /api/pacientes?buscar={texto}` — buscador de pacientes
- `GET /api/pacientes/count` — total de pacientes del resumen
- `POST /api/citas` — crear una cita
- `POST /api/pacientes` — crear un paciente

## Próximo paso sugerido
Definir las páginas de Nueva cita y Nuevo paciente, y conectar `GET /api/citas/hoy` para reemplazar los datos de ejemplo.

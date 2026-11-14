-- ============================================================
-- Consultorio Dental — Tablas principales (SQL Server)
-- Base de datos: ConsultorioDental
-- Uso: ejecuta este script en SQL Server Management Studio
--      o con sqlcmd. Es idempotente para las tablas: si una
--      tabla ya existe, no se vuelve a crear.
-- ============================================================

IF DB_ID('ConsultorioDental') IS NULL
BEGIN
    CREATE DATABASE ConsultorioDental;
END
GO

USE ConsultorioDental;
GO

-- ------------------------------------------------------------
-- Dentistas
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Dentistas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Dentistas (
        DentistaId     INT IDENTITY(1,1) PRIMARY KEY,
        Nombre         NVARCHAR(100) NOT NULL,
        Apellido       NVARCHAR(100) NOT NULL,
        Especialidad   NVARCHAR(100) NOT NULL, -- Ej.: Ortodoncia, Endodoncia, General
        Telefono       NVARCHAR(20)  NULL,
        Email          NVARCHAR(150) NULL,
        Activo         BIT NOT NULL DEFAULT 1,
        FechaCreacion  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

-- ------------------------------------------------------------
-- Pacientes
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Pacientes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pacientes (
        PacienteId      INT IDENTITY(1,1) PRIMARY KEY,
        Nombre          NVARCHAR(100) NOT NULL,
        Apellido        NVARCHAR(100) NOT NULL,
        FechaNacimiento DATE NULL,
        Telefono        NVARCHAR(20)  NULL,
        Email           NVARCHAR(150) NULL,
        Direccion       NVARCHAR(250) NULL,
        Activo          BIT NOT NULL DEFAULT 1,
        FechaRegistro   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_Pacientes_Apellido_Nombre ON dbo.Pacientes (Apellido, Nombre);
    CREATE INDEX IX_Pacientes_Telefono ON dbo.Pacientes (Telefono);
END
GO

-- ------------------------------------------------------------
-- Tratamientos (catálogo de servicios del consultorio)
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Tratamientos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tratamientos (
        TratamientoId     INT IDENTITY(1,1) PRIMARY KEY,
        Nombre            NVARCHAR(150) NOT NULL,
        Descripcion       NVARCHAR(500) NULL,
        Precio            DECIMAL(10,2) NOT NULL CHECK (Precio >= 0),
        DuracionMinutos   INT NOT NULL DEFAULT 30 CHECK (DuracionMinutos > 0),
        Activo            BIT NOT NULL DEFAULT 1
    );
END
GO

-- ------------------------------------------------------------
-- Citas
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Citas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Citas (
        CitaId           INT IDENTITY(1,1) PRIMARY KEY,
        PacienteId       INT NOT NULL,
        DentistaId       INT NOT NULL,
        FechaHora        DATETIME2 NOT NULL,
        DuracionMinutos  INT NOT NULL DEFAULT 30 CHECK (DuracionMinutos > 0),
        Estado           NVARCHAR(20) NOT NULL DEFAULT 'Programada'
                         CHECK (Estado IN ('Programada','Confirmada','Completada','Cancelada','NoAsistio')),
        Motivo           NVARCHAR(250) NULL,
        Notas            NVARCHAR(500) NULL,
        FechaCreacion    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_Citas_Paciente  FOREIGN KEY (PacienteId) REFERENCES dbo.Pacientes (PacienteId),
        CONSTRAINT FK_Citas_Dentista  FOREIGN KEY (DentistaId) REFERENCES dbo.Dentistas (DentistaId)
    );

    CREATE INDEX IX_Citas_FechaHora ON dbo.Citas (FechaHora);
    CREATE INDEX IX_Citas_Paciente_Fecha ON dbo.Citas (PacienteId, FechaHora);
    CREATE INDEX IX_Citas_Dentista_Fecha ON dbo.Citas (DentistaId, FechaHora);
END
GO

-- ------------------------------------------------------------
-- CitaTratamientos (qué tratamientos se hicieron en cada cita)
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.CitaTratamientos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CitaTratamientos (
        CitaTratamientoId INT IDENTITY(1,1) PRIMARY KEY,
        CitaId            INT NOT NULL,
        TratamientoId     INT NOT NULL,
        Precio            DECIMAL(10,2) NOT NULL CHECK (Precio >= 0),
        Cantidad          INT NOT NULL DEFAULT 1 CHECK (Cantidad > 0),

        CONSTRAINT FK_CitaTratamientos_Cita FOREIGN KEY (CitaId)
            REFERENCES dbo.Citas (CitaId),
        CONSTRAINT FK_CitaTratamientos_Tratamiento FOREIGN KEY (TratamientoId)
            REFERENCES dbo.Tratamientos (TratamientoId),
        CONSTRAINT UQ_CitaTratamientos_Cita_Tratamiento UNIQUE (CitaId, TratamientoId)
    );
END
GO

-- ------------------------------------------------------------
-- Pagos
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Pagos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pagos (
        PagoId       INT IDENTITY(1,1) PRIMARY KEY,
        CitaId       INT NOT NULL,
        Monto        DECIMAL(10,2) NOT NULL CHECK (Monto > 0),
        FechaPago    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        MetodoPago   NVARCHAR(30) NOT NULL DEFAULT 'Efectivo'
                     CHECK (MetodoPago IN ('Efectivo','Tarjeta','Transferencia','Otro')),
        Estado       NVARCHAR(20) NOT NULL DEFAULT 'Pagado'
                     CHECK (Estado IN ('Pagado','Pendiente','Reembolsado')),

        CONSTRAINT FK_Pagos_Cita FOREIGN KEY (CitaId) REFERENCES dbo.Citas (CitaId)
    );

    CREATE INDEX IX_Pagos_Cita ON dbo.Pagos (CitaId);
END
GO

-- ============================================================
-- Datos de ejemplo (solo si las tablas están vacías)
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM dbo.Dentistas)
BEGIN
    INSERT INTO dbo.Dentistas (Nombre, Apellido, Especialidad, Telefono, Email)
    VALUES
        ('Ana', 'Pérez', 'General', '555-0101', 'ana.perez@consultorio.test'),
        ('Luis', 'Gómez', 'Ortodoncia', '555-0102', 'luis.gomez@consultorio.test'),
        ('Mario', 'Armenta', 'Endodoncia', '555-0103', 'mario.armenta@consultorio.test');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Tratamientos)
BEGIN
    INSERT INTO dbo.Tratamientos (Nombre, Descripcion, Precio, DuracionMinutos)
    VALUES
        ('Limpieza dental', 'Limpieza y pulido dental', 800.00, 45),
        ('Revisión general', 'Revisión y diagnóstico general', 500.00, 30),
        ('Ortodoncia — revisión', 'Revisión y ajuste de ortodoncia', 1200.00, 60);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Pacientes)
BEGIN
    INSERT INTO dbo.Pacientes (Nombre, Apellido, FechaNacimiento, Telefono, Email)
    VALUES
        ('María', 'López', '1990-05-12', '555-1001', 'maria.lopez@test.com'),
        ('Carlos', 'Ruiz', '1985-09-03', '555-1002', 'carlos.ruiz@test.com'),
        ('Sofía', 'Mendoza', '1998-02-20', '555-1003', 'sofia.mendoza@test.com');
END
GO

-- Citas de ejemplo para hoy (solo si no hay citas)
IF NOT EXISTS (SELECT 1 FROM dbo.Citas)
BEGIN
    INSERT INTO dbo.Citas (PacienteId, DentistaId, FechaHora, DuracionMinutos, Estado, Motivo)
    VALUES
        (1, 1, DATEADD(HOUR, 9, CAST(CAST(GETDATE() AS DATE) AS DATETIME2)), 45, 'Confirmada', 'Limpieza dental'),
        (2, 2, DATEADD(HOUR, 10, CAST(CAST(GETDATE() AS DATE) AS DATETIME2)), 60, 'Programada', 'Ortodoncia — revisión'),
        (3, 3, DATEADD(HOUR, 11, CAST(CAST(GETDATE() AS DATE) AS DATETIME2)), 30, 'Confirmada', 'Revisión general');
END
GO

-- Resumen rápido para comprobar
SELECT 'Dentistas' AS Tabla, COUNT(*) AS Registros FROM dbo.Dentistas
UNION ALL SELECT 'Pacientes', COUNT(*) FROM dbo.Pacientes
UNION ALL SELECT 'Tratamientos', COUNT(*) FROM dbo.Tratamientos
UNION ALL SELECT 'Citas', COUNT(*) FROM dbo.Citas;
GO

-- ============================================================
-- 003-Bitacora.sql — Bitácora de cambios (2026-11-21)
-- Para bases que ya existen: ejecuta este script sobre la base
-- ConsultorioDental. Es idempotente: si la tabla ya existe, no
-- hace nada. Requiere la tabla Usuarios (002-Usuarios.sql).
-- ============================================================

USE ConsultorioDental;
GO

IF OBJECT_ID('dbo.Bitacora', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bitacora (
        BitacoraId     INT IDENTITY(1,1) PRIMARY KEY,
        FechaHora      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UsuarioId      INT NOT NULL,
        UsuarioNombre  NVARCHAR(100) NOT NULL,
        Accion         NVARCHAR(30) NOT NULL,
        Entidad        NVARCHAR(30) NOT NULL,
        EntidadId      INT NOT NULL,
        Detalle        NVARCHAR(500) NOT NULL DEFAULT '',

        CONSTRAINT FK_Bitacora_Usuario FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId)
    );

    CREATE INDEX IX_Bitacora_FechaHora ON dbo.Bitacora (FechaHora DESC);
    CREATE INDEX IX_Bitacora_Entidad ON dbo.Bitacora (Entidad, EntidadId);
END
GO

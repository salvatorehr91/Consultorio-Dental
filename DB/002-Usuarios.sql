-- ============================================================
-- 002-Usuarios.sql — Login de administrador (2026-11-20)
-- Para bases que ya existen: ejecuta este script sobre la base
-- ConsultorioDental. Es idempotente: si la tabla ya existe, no
-- hace nada. El usuario inicial ("admin") lo crea el API al
-- arrancar si la tabla está vacía (ver DbSeeder.cs).
-- ============================================================

USE ConsultorioDental;
GO

IF OBJECT_ID('dbo.Usuarios', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios (
        UsuarioId      INT IDENTITY(1,1) PRIMARY KEY,
        NombreUsuario  NVARCHAR(100) NOT NULL,
        PasswordHash   NVARCHAR(500) NOT NULL,
        Rol            NVARCHAR(20) NOT NULL DEFAULT 'Recepcionista'
                       CHECK (Rol IN ('Admin','Recepcionista')),
        Activo         BIT NOT NULL DEFAULT 1,
        FechaCreacion  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT UQ_Usuarios_NombreUsuario UNIQUE (NombreUsuario)
    );

    CREATE INDEX IX_Usuarios_NombreUsuario ON dbo.Usuarios (NombreUsuario);
END
GO

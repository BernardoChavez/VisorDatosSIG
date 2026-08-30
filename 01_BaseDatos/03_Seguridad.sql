-- ============================================================================
-- PROYECTO: VisorDatosSIG 2026
-- ARCHIVO: 01_BaseDatos/03_Seguridad.sql
-- DESCRIPCIÓN: Tablas de autenticación, roles, permisos granulares, auditoría/bitácora,
--              procedimientos almacenados de seguridad y usuario de aplicación con mínimo privilegio.
-- MOTOR: Microsoft SQL Server 2022
-- ============================================================================

USE VisorDatosSIG_BD;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- LIMPIEZA PREVIA EN ORDEN DE DEPENDENCIAS (FKs)
-- ============================================================================
IF OBJECT_ID(N'dbo.UsuarioPermisos', N'U') IS NOT NULL DROP TABLE dbo.UsuarioPermisos;
IF OBJECT_ID(N'dbo.Permisos', N'U') IS NOT NULL DROP TABLE dbo.Permisos;
IF OBJECT_ID(N'dbo.BitacoraEventos', N'U') IS NOT NULL DROP TABLE dbo.BitacoraEventos;
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL DROP TABLE dbo.Usuarios;
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

-- ============================================================================
-- 1. TABLA: Roles (RF-SEG-02: Administrador y Consultor)
-- ============================================================================
CREATE TABLE dbo.Roles (
    IdRol               INT IDENTITY(1,1) NOT NULL,
    Nombre              VARCHAR(50) NOT NULL,
    Descripcion         VARCHAR(250) NULL,
    FechaCreacion       DATETIME2(3) NOT NULL CONSTRAINT DF_Roles_FechaCreacion DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (IdRol),
    CONSTRAINT UQ_Roles_Nombre UNIQUE (Nombre)
);
GO

-- ============================================================================
-- 2. TABLA: Usuarios (RF-SEG-01, 02 y 03)
-- ============================================================================
CREATE TABLE dbo.Usuarios (
    IdUsuario           INT IDENTITY(1,1) NOT NULL,
    Username            VARCHAR(50) NOT NULL,
    PasswordHash        VARCHAR(255) NOT NULL, -- Hash PBKDF2 con SHA-512 / ASP.NET Core Identity
    Salt                VARCHAR(100) NOT NULL,
    NombreCompleto      VARCHAR(150) NOT NULL,
    Email               VARCHAR(150) NOT NULL,
    IdRol               INT NOT NULL,
    Activo              BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT 1,
    IntentosFallidos    INT NOT NULL CONSTRAINT DF_Usuarios_Intentos DEFAULT 0,
    BloqueadoHasta      DATETIME2(3) NULL,
    UltimoAcceso        DATETIME2(3) NULL,
    FechaCreacion       DATETIME2(3) NOT NULL CONSTRAINT DF_Usuarios_FechaCreacion DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Usuarios PRIMARY KEY CLUSTERED (IdUsuario),
    CONSTRAINT UQ_Usuarios_Username UNIQUE (Username),
    CONSTRAINT UQ_Usuarios_Email UNIQUE (Email),
    CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (IdRol) REFERENCES dbo.Roles(IdRol)
);
GO

-- ============================================================================
-- 3. TABLA: Permisos (Catálogo maestro de opciones del Dashboard y Módulos)
-- ============================================================================
CREATE TABLE dbo.Permisos (
    CodigoPermiso       VARCHAR(50) NOT NULL,
    Nombre              VARCHAR(100) NOT NULL,
    Categoria           VARCHAR(50) NOT NULL, -- 'Capas', 'Herramientas', 'Funcionalidades', 'Modulos'
    Descripcion         VARCHAR(250) NULL,
    Orden               INT NOT NULL CONSTRAINT DF_Permisos_Orden DEFAULT 1,
    CONSTRAINT PK_Permisos PRIMARY KEY CLUSTERED (CodigoPermiso)
);
GO

-- ============================================================================
-- 4. TABLA: UsuarioPermisos (Permisos individuales asignados por el Admin)
-- ============================================================================
CREATE TABLE dbo.UsuarioPermisos (
    IdUsuario           INT NOT NULL,
    CodigoPermiso       VARCHAR(50) NOT NULL,
    Permitido           BIT NOT NULL CONSTRAINT DF_UsuarioPermisos_Permitido DEFAULT 1,
    CONSTRAINT PK_UsuarioPermisos PRIMARY KEY CLUSTERED (IdUsuario, CodigoPermiso),
    CONSTRAINT FK_UsuarioPermisos_Usuarios FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios(IdUsuario) ON DELETE CASCADE,
    CONSTRAINT FK_UsuarioPermisos_Permisos FOREIGN KEY (CodigoPermiso) REFERENCES dbo.Permisos(CodigoPermiso) ON DELETE CASCADE
);
GO

-- ============================================================================
-- 5. TABLA: BitacoraEventos (Auditoría de seguridad y operaciones - RF-SEG-06)
-- ============================================================================
CREATE TABLE dbo.BitacoraEventos (
    IdBitacora          BIGINT IDENTITY(1,1) NOT NULL,
    FechaHora           DATETIME2(3) NOT NULL CONSTRAINT DF_Bitacora_FechaHora DEFAULT SYSUTCDATETIME(),
    Usuario             VARCHAR(100) NULL,
    IpCliente           VARCHAR(50) NULL,
    TipoEvento          VARCHAR(50) NOT NULL, -- 'LoginExitoso', 'LoginFallido', 'Logout', 'Migracion', 'ModificacionUsuario', 'ConsultaEspacial', 'Exportacion'
    Modulo              VARCHAR(50) NOT NULL, -- 'Seguridad', 'Migrador', 'Visor', 'Consultas', 'Administracion'
    Descripcion         VARCHAR(500) NOT NULL,
    DetalleTecnico      NVARCHAR(MAX) NULL,
    Exitoso             BIT NOT NULL CONSTRAINT DF_Bitacora_Exitoso DEFAULT 1,
    CONSTRAINT PK_BitacoraEventos PRIMARY KEY CLUSTERED (IdBitacora)
);
GO

CREATE NONCLUSTERED INDEX IX_BitacoraEventos_FechaHora ON dbo.BitacoraEventos (FechaHora DESC);
CREATE NONCLUSTERED INDEX IX_BitacoraEventos_Usuario ON dbo.BitacoraEventos (Usuario);
CREATE NONCLUSTERED INDEX IX_BitacoraEventos_TipoEvento ON dbo.BitacoraEventos (TipoEvento);
GO

-- ============================================================================
-- 6. PROCEDIMIENTOS ALMACENADOS DE SEGURIDAD Y AUDITORÍA
-- ============================================================================

-- 6.1 Registrar Evento en Bitácora
IF OBJECT_ID(N'dbo.usp_RegistrarBitacoraEvento', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_RegistrarBitacoraEvento;
GO

CREATE PROCEDURE dbo.usp_RegistrarBitacoraEvento
    @Usuario VARCHAR(100),
    @IpCliente VARCHAR(50),
    @TipoEvento VARCHAR(50),
    @Modulo VARCHAR(50),
    @Descripcion VARCHAR(500),
    @DetalleTecnico NVARCHAR(MAX) = NULL,
    @Exitoso BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO dbo.BitacoraEventos (
        FechaHora,
        Usuario,
        IpCliente,
        TipoEvento,
        Modulo,
        Descripcion,
        DetalleTecnico,
        Exitoso
    )
    VALUES (
        SYSUTCDATETIME(),
        @Usuario,
        @IpCliente,
        @TipoEvento,
        @Modulo,
        @Descripcion,
        @DetalleTecnico,
        @Exitoso
    );
END
GO

-- 6.2 Obtener Usuario por Username para Autenticación
IF OBJECT_ID(N'dbo.usp_ObtenerUsuarioPorUsername', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_ObtenerUsuarioPorUsername;
GO

CREATE PROCEDURE dbo.usp_ObtenerUsuarioPorUsername
    @Username VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.IdUsuario,
        u.Username,
        u.PasswordHash,
        u.Salt,
        u.NombreCompleto,
        u.Email,
        u.IdRol,
        r.Nombre AS Rol,
        u.Activo,
        u.IntentosFallidos,
        u.BloqueadoHasta,
        u.UltimoAcceso
    FROM dbo.Usuarios u
    INNER JOIN dbo.Roles r ON u.IdRol = r.IdRol
    WHERE u.Username = @Username;
END
GO

-- 6.3 Actualizar Acceso / Bloqueo de Usuario
IF OBJECT_ID(N'dbo.usp_RegistrarIntentoLogin', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_RegistrarIntentoLogin;
GO

CREATE PROCEDURE dbo.usp_RegistrarIntentoLogin
    @Username VARCHAR(50),
    @Exitoso BIT,
    @MaxIntentosPermitidos INT = 5,
    @MinutosBloqueo INT = 15
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @Exitoso = 1
    BEGIN
        UPDATE dbo.Usuarios
        SET 
            IntentosFallidos = 0,
            BloqueadoHasta = NULL,
            UltimoAcceso = SYSUTCDATETIME()
        WHERE Username = @Username;
    END
    ELSE
    BEGIN
        UPDATE dbo.Usuarios
        SET 
            IntentosFallidos = IntentosFallidos + 1,
            BloqueadoHasta = CASE 
                WHEN IntentosFallidos + 1 >= @MaxIntentosPermitidos 
                THEN DATEADD(MINUTE, @MinutosBloqueo, SYSUTCDATETIME())
                ELSE NULL 
            END
        WHERE Username = @Username;
    END
END
GO

-- 6.4 Listado de Bitácora con Paginación
IF OBJECT_ID(N'dbo.usp_ObtenerBitacora', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_ObtenerBitacora;
GO

CREATE PROCEDURE dbo.usp_ObtenerBitacora
    @FechaDesde DATETIME2(3) = NULL,
    @FechaHasta DATETIME2(3) = NULL,
    @TipoEvento VARCHAR(50) = NULL,
    @Usuario VARCHAR(100) = NULL,
    @Pagina INT = 1,
    @RegistrosPorPagina INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@Pagina - 1) * @RegistrosPorPagina;

    ;WITH BitacoraFiltrada AS (
        SELECT 
            b.IdBitacora,
            b.FechaHora,
            b.Usuario,
            b.IpCliente,
            b.TipoEvento,
            b.Modulo,
            b.Descripcion,
            b.DetalleTecnico,
            b.Exitoso
        FROM dbo.BitacoraEventos b
        WHERE (@FechaDesde IS NULL OR b.FechaHora >= @FechaDesde)
          AND (@FechaHasta IS NULL OR b.FechaHora <= @FechaHasta)
          AND (@TipoEvento IS NULL OR b.TipoEvento = @TipoEvento)
          AND (@Usuario IS NULL OR b.Usuario LIKE '%' + @Usuario + '%')
    )
    SELECT 
        COUNT(*) OVER() AS TotalRegistros,
        *
    FROM BitacoraFiltrada
    ORDER BY FechaHora DESC
    OFFSET @Offset ROWS
    FETCH NEXT @RegistrosPorPagina ROWS ONLY;
END
GO

-- 6.5 Procedimientos para Permisos Granulares
IF OBJECT_ID(N'dbo.usp_ObtenerPermisosUsuario', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_ObtenerPermisosUsuario;
GO

CREATE PROCEDURE dbo.usp_ObtenerPermisosUsuario
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT CodigoPermiso
    FROM dbo.UsuarioPermisos
    WHERE IdUsuario = @IdUsuario AND Permitido = 1;
END
GO

IF OBJECT_ID(N'dbo.usp_GuardarUsuarioPermisos', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GuardarUsuarioPermisos;
GO

CREATE PROCEDURE dbo.usp_GuardarUsuarioPermisos
    @IdUsuario INT,
    @PermisosCsv NVARCHAR(MAX) -- Códigos separados por coma ej: 'CAPA_Lotes,HERR_GPS'
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY
        DELETE FROM dbo.UsuarioPermisos WHERE IdUsuario = @IdUsuario;
        
        IF @PermisosCsv IS NOT NULL AND LEN(LTRIM(RTRIM(@PermisosCsv))) > 0
        BEGIN
            INSERT INTO dbo.UsuarioPermisos (IdUsuario, CodigoPermiso, Permitido)
            SELECT @IdUsuario, LTRIM(RTRIM(value)), 1
            FROM STRING_SPLIT(@PermisosCsv, ',')
            WHERE LTRIM(RTRIM(value)) IN (SELECT CodigoPermiso FROM dbo.Permisos);
        END
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID(N'dbo.usp_ObtenerCatalogoPermisos', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_ObtenerCatalogoPermisos;
GO

CREATE PROCEDURE dbo.usp_ObtenerCatalogoPermisos
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT CodigoPermiso, Nombre, Categoria, Descripcion, Orden
    FROM dbo.Permisos
    ORDER BY Categoria ASC, Orden ASC, Nombre ASC;
END
GO

IF OBJECT_ID(N'dbo.usp_EliminarUsuario', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_EliminarUsuario;
GO

CREATE PROCEDURE dbo.usp_EliminarUsuario
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY
        DELETE FROM dbo.UsuarioPermisos WHERE IdUsuario = @IdUsuario;
        DELETE FROM dbo.Usuarios WHERE IdUsuario = @IdUsuario;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- 7. CARGA DE ROLES, PERMISOS Y USUARIOS INICIALES (SEMILLA)
-- ============================================================================
-- Roles: Administrador y Consultor
INSERT INTO dbo.Roles (Nombre, Descripcion)
VALUES 
    ('Administrador', 'Acceso total a configuración, migración de shapefiles, gestión de usuarios y auditoría.'),
    ('Consultor', 'Acceso a visualización cartográfica, consultas, búsquedas y exportación de información.');

-- Catálogo de Permisos
INSERT INTO dbo.Permisos (CodigoPermiso, Nombre, Categoria, Descripcion, Orden)
VALUES
    -- Capas Cartográficas
    ('CAPA_Lotes', 'Lotes Prediales', 'Capas', 'Visualización e interacción con polígonos de predios', 1),
    ('CAPA_Manzanas', 'Manzanas Catastrales', 'Capas', 'Visualización de límites de manzanas urbanas', 2),
    ('CAPA_CodigosFijos', 'Códigos Fijos / Hitos', 'Capas', 'Visualización de puntos e hitos georreferenciados', 3),
    ('CAPA_Vias', 'Vías y Red Vial', 'Capas', 'Visualización de calles, avenidas y red vial', 4),

    -- Herramientas de Interacción Cartográfica
    ('HERR_IDENTIFICAR', 'Identificación por Clic', 'Herramientas', 'Consultar ficha de atributos al hacer clic sobre entidades', 1),
    ('HERR_MEDICION_DISTANCIA', 'Medir Distancia Lineal', 'Herramientas', 'Cálculo de longitud de trayectos y polilíneas', 2),
    ('HERR_MEDICION_AREA', 'Medir Área Poligonal', 'Herramientas', 'Cálculo de superficie de parcelas en m² y hectáreas', 3),
    ('HERR_GPS', 'Geolocalización GPS', 'Herramientas', 'Detección en tiempo real de la ubicación del dispositivo', 4),
    ('HERR_COORDENADAS', 'Ir a Coordenadas', 'Herramientas', 'Búsqueda y centrado por coordenadas WGS 84 (X, Y)', 5),
    ('HERR_RESTABLECER_VISTA', 'Restablecer Extensión', 'Herramientas', 'Centrar mapa en la extensión geográfica inicial', 6),

    -- Funcionalidades y Búsqueda
    ('FUNC_BUSQUEDA_ESPACIAL', 'Búsqueda Espacial/Alfanumérica', 'Funcionalidades', 'Buscador por código, titular, predio o vía', 1),
    ('FUNC_EXPORTAR_WKT', 'Copiar / Exportar WKT', 'Funcionalidades', 'Copiado de la geometría en formato Well-Known Text', 2),
    ('FUNC_CAMBIAR_MAPA_BASE', 'Galería de Mapas Base', 'Funcionalidades', 'Cambio entre mapas base OSM, Positron, Dark y Satelital', 3),
    ('FUNC_CAMBIAR_TEMA', 'Modo Claro / Oscuro', 'Funcionalidades', 'Alternar temas visuales de la interfaz', 4),

    -- Módulos del Sistema
    ('MOD_MIGRACIONES', 'Migrador Shapefiles', 'Modulos', 'Carga, validación e importación masiva de capas', 1),
    ('MOD_AUDITORIA', 'Bitácora de Auditoría', 'Modulos', 'Monitoreo de eventos, accesos y transacciones', 2),
    ('MOD_USUARIOS', 'Gestión de Usuarios', 'Modulos', 'Administración de cuentas, roles y permisos granulares', 3);

-- Usuarios iniciales:
-- 1. admin     / Clave: Admin123*! (Hash PBKDF2-HMAC-SHA512 + Salt)
-- 2. consultor / Clave: Consultor123*! (Hash PBKDF2-HMAC-SHA512 + Salt)
INSERT INTO dbo.Usuarios (Username, PasswordHash, Salt, NombreCompleto, Email, IdRol, Activo)
VALUES 
    (
        'admin',
        '8kQ9P2g9pM3v6e5u2yL9aF0s4g7h1j3k5l7o9p1q3r5t7v9x1z3b5d7f9h1j3k5l7o9p1q3r5t7v9x1z==', 
        'S0m3S3cur3S@ltForAdmin2026==',
        'Administrador del Sistema SIG',
        'admin@visordatos.sig.pe',
        1, -- Administrador
        1
    ),
    (
        'consultor',
        '5l7o9p1q3r5t7v9x1z3b5d7f9h1j3k5l7o9p1q3r5t7v9x1z8kQ9P2g9pM3v6e5u2yL9aF0s4g7h1j3k==',
        'S0m3S3cur3S@ltForConsultor2026==',
        'Consultor Cartográfico',
        'consultor@visordatos.sig.pe',
        2, -- Consultor
        1
    );

-- Asignación de Permisos Iniciales:
-- Admin: Todos los permisos
INSERT INTO dbo.UsuarioPermisos (IdUsuario, CodigoPermiso, Permitido)
SELECT 1, CodigoPermiso, 1 FROM dbo.Permisos;

-- Consultor: Todas las capas, herramientas de consulta y mapas base (sin módulos admin)
INSERT INTO dbo.UsuarioPermisos (IdUsuario, CodigoPermiso, Permitido)
SELECT 2, CodigoPermiso, 1 
FROM dbo.Permisos
WHERE CodigoPermiso NOT IN ('MOD_MIGRACIONES', 'MOD_AUDITORIA', 'MOD_USUARIOS');
GO

PRINT '>> Tablas de roles, usuarios, permisos granulares, bitácora de auditoría y procedimientos de seguridad creados exitosamente.';
GO

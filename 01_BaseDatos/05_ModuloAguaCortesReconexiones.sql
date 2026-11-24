-- ============================================================================
-- PROYECTO: VisorDatosSIG 2026 — SEGUNDA PARTE (ALCANCE SIG 2/2026)
-- ARCHIVO: 01_BaseDatos/05_ModuloAguaCortesReconexiones.sql
-- DESCRIPCIÓN: Módulo de Gestión de Cortes y Reconexiones de Servicios de Agua Potable:
--              1. Conexiones de Agua Potable con Estados (Normal, ParaCorte, Cortado, BajaParcial)
--              2. Trabajadores Operarios clasificados como Cortador o Reconector
--              3. Asignaciones de Rutas Óptimas con Red de Calles
--              4. Detalle de Puntos de Ruta (1, 2, 3...) y Registro en Campo (Foto, Lectura, GPS, Observación)
--              5. Monitoreo en Tiempo Real de Operarios
--              6. Procedimientos Almacenados de Dashboard, Asignación y Auditoría GPS
-- MOTOR: Microsoft SQL Server 2022 Spatial (SRID 4326 - WGS 84)
-- ============================================================================

USE VisorDatosSIG_BD;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '>> Iniciando despliegue de estructuras de datos para el Módulo de Cortes y Reconexiones de Agua...';
GO

-- ============================================================================
-- 1. LIMPIEZA PREVIA SI EXISTEN (Para permitir re-ejecución limpia)
-- ============================================================================
IF OBJECT_ID(N'dbo.MonitoreoTrabajador', N'U') IS NOT NULL DROP TABLE dbo.MonitoreoTrabajador;
IF OBJECT_ID(N'dbo.AsignacionDetalle', N'U') IS NOT NULL DROP TABLE dbo.AsignacionDetalle;
IF OBJECT_ID(N'dbo.Asignaciones', N'U') IS NOT NULL DROP TABLE dbo.Asignaciones;
IF OBJECT_ID(N'dbo.Trabajadores', N'U') IS NOT NULL DROP TABLE dbo.Trabajadores;
IF OBJECT_ID(N'dbo.ConexionesAgua', N'U') IS NOT NULL DROP TABLE dbo.ConexionesAgua;
GO

-- Registrar la capa de Conexiones de Agua en el catálogo general si no existiese
IF NOT EXISTS (SELECT 1 FROM dbo.CatalogoCapas WHERE Nombre = 'ConexionesAgua')
BEGIN
    INSERT INTO dbo.CatalogoCapas 
    (Nombre, Titulo, Descripcion, TipoGeometria, ColorBorde, ColorFondo, Opacidad, GrosorBorde, OrdenVisualizacion, Activo, Icono, CamposVisibles)
    VALUES 
    ('ConexionesAgua', 'Conexiones y Medidores de Agua', 'Medidores georreferenciados clasificados por estado (Normal, Para Corte, Cortado, Baja Parcial)', 'Point', '#0284c7', '#38bdf8', 0.95, 3, 5, 1, 'bi-droplet-half',
     '[{"campo":"CodigoConexion","alias":"Cód. Conexión"},{"campo":"NumeroMedidor","alias":"Nº Medidor"},{"campo":"Titular","alias":"Titular"},{"campo":"EstadoConexion","alias":"Estado"},{"campo":"MesesMora","alias":"Meses Mora"},{"campo":"SaldoPendiente","alias":"Saldo Pendiente"},{"campo":"Direccion","alias":"Dirección"}]');
END;
GO

-- ============================================================================
-- 2. TABLA: ConexionesAgua (Puntos de conexión en el territorio)
-- ============================================================================
CREATE TABLE dbo.ConexionesAgua (
    IdConexion              INT IDENTITY(1,1) NOT NULL,
    CodigoConexion          VARCHAR(50) NOT NULL,            -- Clave natural ej. 'CON-1001'
    NumeroMedidor           VARCHAR(50) NOT NULL,            -- Ej. 'MED-5542'
    IdLote                  INT NULL,                        -- Vinculación catastral al lote
    CodigoLote              VARCHAR(50) NULL,
    CodigoManzana           VARCHAR(50) NULL,
    Titular                 VARCHAR(150) NOT NULL,           -- Nombre del cliente / abonado
    Direccion               VARCHAR(200) NOT NULL,           -- Dirección predial
    Categoria               VARCHAR(50) NOT NULL CONSTRAINT DF_Conexiones_Cat DEFAULT 'Doméstico',
    EstadoConexion          VARCHAR(30) NOT NULL CONSTRAINT DF_Conexiones_Estado DEFAULT 'Normal',
                            -- Estados obligatorios según especificación:
                            -- 'Normal'     -> 🟢 Verde (Al día)
                            -- 'ParaCorte'  -> 🟡 Amarillo/Naranja (Con orden de corte por mora)
                            -- 'Cortado'    -> 🔴 Rojo (Servicio suspendido)
                            -- 'BajaParcial'-> ⚪ Azul/Gris (Medidor precintado o retirado)
    MesesMora               INT NOT NULL CONSTRAINT DF_Conexiones_Mora DEFAULT 0,
    SaldoPendiente          DECIMAL(12,2) NOT NULL CONSTRAINT DF_Conexiones_Saldo DEFAULT 0.00,
    UltimaLectura           DECIMAL(10,2) NOT NULL CONSTRAINT DF_Conexiones_Lectura DEFAULT 0.00,
    FechaUltimaLectura      DATETIME2(3) NULL,
    Longitud                FLOAT NOT NULL,
    Latitud                 FLOAT NOT NULL,
    Geometria               GEOMETRY NOT NULL,               -- Point SRID 4326
    FechaRegistro           DATETIME2(3) NOT NULL CONSTRAINT DF_Conexiones_Fecha DEFAULT SYSUTCDATETIME(),
    Activo                  BIT NOT NULL CONSTRAINT DF_Conexiones_Activo DEFAULT 1,
    
    CONSTRAINT PK_ConexionesAgua PRIMARY KEY CLUSTERED (IdConexion),
    CONSTRAINT UQ_ConexionesAgua_Codigo UNIQUE (CodigoConexion),
    CONSTRAINT UQ_ConexionesAgua_Medidor UNIQUE (NumeroMedidor),
    CONSTRAINT FK_ConexionesAgua_Lotes FOREIGN KEY (IdLote) REFERENCES dbo.Lotes(IdLote) ON DELETE SET NULL,
    CONSTRAINT CK_ConexionesAgua_Estado CHECK (EstadoConexion IN ('Normal', 'ParaCorte', 'Cortado', 'BajaParcial')),
    CONSTRAINT CK_ConexionesAgua_SRID CHECK (Geometria.STSrid = 4326),
    CONSTRAINT CK_ConexionesAgua_Valida CHECK (Geometria.STIsValid() = 1)
);
GO

-- Índice espacial para filtrado territorial por BoundingBox en Leaflet
CREATE SPATIAL INDEX SPDX_ConexionesAgua_Geometria
ON dbo.ConexionesAgua(Geometria)
USING GEOMETRY_GRID
WITH (
    BOUNDING_BOX = (-180, -90, 180, 90),
    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = HIGH, LEVEL_4 = HIGH),
    CELLS_PER_OBJECT = 16
);
GO

CREATE NONCLUSTERED INDEX IX_ConexionesAgua_Estado ON dbo.ConexionesAgua (EstadoConexion) INCLUDE (CodigoConexion, Titular, NumeroMedidor);
CREATE NONCLUSTERED INDEX IX_ConexionesAgua_Titular ON dbo.ConexionesAgua (Titular);
CREATE NONCLUSTERED INDEX IX_ConexionesAgua_CodigoLote ON dbo.ConexionesAgua (CodigoLote);
GO

-- ============================================================================
-- 3. TABLA: Trabajadores (Operarios clasificados como Cortador o Reconector)
-- ============================================================================
CREATE TABLE dbo.Trabajadores (
    IdTrabajador            INT IDENTITY(1,1) NOT NULL,
    CodigoTrabajador        VARCHAR(50) NOT NULL,            -- Ej. 'TRAB-001'
    NombreCompleto          VARCHAR(150) NOT NULL,
    DocumentoIdentidad      VARCHAR(30) NULL,
    Telefono                VARCHAR(30) NULL,
    TipoTrabajador          VARCHAR(30) NOT NULL,            -- 'Cortador' o 'Reconector'
    Estado                  VARCHAR(30) NOT NULL CONSTRAINT DF_Trabajadores_Estado DEFAULT 'Disponible',
                            -- 'Disponible', 'EnRuta', 'Inactivo'
    Vehiculo                VARCHAR(50) NOT NULL CONSTRAINT DF_Trabajadores_Vehiculo DEFAULT 'Moto',
                            -- 'Moto', 'A pie', 'Vehículo'
    IdUsuario               INT NULL,                        -- Vinculación opcional a credencial de acceso
    FechaRegistro           DATETIME2(3) NOT NULL CONSTRAINT DF_Trabajadores_Fecha DEFAULT SYSUTCDATETIME(),
    Activo                  BIT NOT NULL CONSTRAINT DF_Trabajadores_Activo DEFAULT 1,

    CONSTRAINT PK_Trabajadores PRIMARY KEY CLUSTERED (IdTrabajador),
    CONSTRAINT UQ_Trabajadores_Codigo UNIQUE (CodigoTrabajador),
    CONSTRAINT FK_Trabajadores_Usuarios FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios(IdUsuario) ON DELETE SET NULL,
    CONSTRAINT CK_Trabajadores_Tipo CHECK (TipoTrabajador IN ('Cortador', 'Reconector')),
    CONSTRAINT CK_Trabajadores_Estado CHECK (Estado IN ('Disponible', 'EnRuta', 'Inactivo'))
);
GO

-- ============================================================================
-- 4. TABLA: Asignaciones (Cabecera de Órdenes de Ruta Calculadas con Dijkstra)
-- ============================================================================
CREATE TABLE dbo.Asignaciones (
    IdAsignacion            INT IDENTITY(1,1) NOT NULL,
    CodigoAsignacion        VARCHAR(50) NOT NULL,            -- Ej. 'ASIG-2026-0001'
    IdTrabajador            INT NOT NULL,
    TipoServicio            VARCHAR(30) NOT NULL,            -- 'Corte' o 'Reconexion'
    FechaAsignacion         DATE NOT NULL CONSTRAINT DF_Asignaciones_Fecha DEFAULT CAST(GETDATE() AS DATE),
    Estado                  VARCHAR(30) NOT NULL CONSTRAINT DF_Asignaciones_Estado DEFAULT 'Pendiente',
                            -- 'Pendiente', 'EnProgreso', 'Completada', 'Cancelada'
    CantidadPuntos          INT NOT NULL DEFAULT 0,
    PuntoPartidaNombre      VARCHAR(150) NOT NULL CONSTRAINT DF_Asignaciones_PtoNom DEFAULT 'Almacén Central / Oficina',
    PuntoPartidaLon         FLOAT NOT NULL,
    PuntoPartidaLat         FLOAT NOT NULL,
    PuntoPartidaGeom        GEOMETRY NULL,                   -- Point SRID 4326
    DistanciaTotalMetros    DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    RutaOptimaWkt           VARCHAR(MAX) NULL,               -- LineString / MultiLineString de la red vial calculada
    FechaCreacion           DATETIME2(3) NOT NULL CONSTRAINT DF_Asignaciones_FechaCrea DEFAULT SYSUTCDATETIME(),
    FechaInicio             DATETIME2(3) NULL,
    FechaFinalizacion       DATETIME2(3) NULL,
    Notas                   VARCHAR(500) NULL,
    UsuarioCreador          VARCHAR(100) NULL,

    CONSTRAINT PK_Asignaciones PRIMARY KEY CLUSTERED (IdAsignacion),
    CONSTRAINT UQ_Asignaciones_Codigo UNIQUE (CodigoAsignacion),
    CONSTRAINT FK_Asignaciones_Trabajador FOREIGN KEY (IdTrabajador) REFERENCES dbo.Trabajadores(IdTrabajador),
    CONSTRAINT CK_Asignaciones_Tipo CHECK (TipoServicio IN ('Corte', 'Reconexion')),
    CONSTRAINT CK_Asignaciones_Estado CHECK (Estado IN ('Pendiente', 'EnProgreso', 'Completada', 'Cancelada'))
);
GO

CREATE NONCLUSTERED INDEX IX_Asignaciones_Trabajador ON dbo.Asignaciones (IdTrabajador, Estado, FechaAsignacion);
GO

-- ============================================================================
-- 5. TABLA: AsignacionDetalle (Puntos ordenados 1, 2, 3... y validación GPS)
-- ============================================================================
CREATE TABLE dbo.AsignacionDetalle (
    IdDetalle               INT IDENTITY(1,1) NOT NULL,
    IdAsignacion            INT NOT NULL,
    IdConexion              INT NOT NULL,
    OrdenSecuencia          INT NOT NULL,                    -- 1, 2, 3... según Dijkstra
    DistanciaDesdeAnteriorM DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    Estado                  VARCHAR(30) NOT NULL CONSTRAINT DF_AsigDet_Estado DEFAULT 'Pendiente',
                            -- 'Pendiente', 'Ejecutado', 'CasaCerrada', 'Imposible', 'Rechazado'
    LecturaAnterior         DECIMAL(10,2) NULL,
    LecturaActual           DECIMAL(10,2) NULL,              -- Lectura registrada en campo
    NumeroMedidorReportado  VARCHAR(50) NULL,                -- Número verificado en el predio
    FotoMedidorUrl          VARCHAR(500) NULL,               -- URL o base64 de la foto tomada
    Observacion             VARCHAR(500) NULL,               -- Ej. 'Casa cerrada', 'Perro bravo'
    FechaHoraEjecucion      DATETIME2(3) NULL,               -- Timestamp automático en campo
    GpsLatEjecucion         FLOAT NULL,                      -- Coordenadas reales del móvil al guardar
    GpsLonEjecucion         FLOAT NULL,
    GpsPrecisionM           FLOAT NULL,
    DistanciaValidacionGpsM DECIMAL(10,2) NULL,              -- Distancia calculada al medidor real (en metros)
    ValidacionGpsExitosa    BIT NULL,                        -- 1 si estuvo en el lugar (<= tolerancia)
    
    CONSTRAINT PK_AsignacionDetalle PRIMARY KEY CLUSTERED (IdDetalle),
    CONSTRAINT UQ_AsigDet_AsigOrden UNIQUE (IdAsignacion, OrdenSecuencia),
    CONSTRAINT UQ_AsigDet_AsigConexion UNIQUE (IdAsignacion, IdConexion),
    CONSTRAINT FK_AsigDet_Asignacion FOREIGN KEY (IdAsignacion) REFERENCES dbo.Asignaciones(IdAsignacion) ON DELETE CASCADE,
    CONSTRAINT FK_AsigDet_Conexion FOREIGN KEY (IdConexion) REFERENCES dbo.ConexionesAgua(IdConexion),
    CONSTRAINT CK_AsigDet_Estado CHECK (Estado IN ('Pendiente', 'Ejecutado', 'CasaCerrada', 'Imposible', 'Rechazado'))
);
GO

CREATE NONCLUSTERED INDEX IX_AsigDet_Asignacion ON dbo.AsignacionDetalle (IdAsignacion, OrdenSecuencia);
GO

-- ============================================================================
-- 6. TABLA: MonitoreoTrabajador (Ubicación en tiempo real para panel de control)
-- ============================================================================
CREATE TABLE dbo.MonitoreoTrabajador (
    IdMonitoreo             INT IDENTITY(1,1) NOT NULL,
    IdTrabajador            INT NOT NULL,
    IdAsignacionActiva      INT NULL,
    Latitud                 FLOAT NOT NULL,
    Longitud                FLOAT NOT NULL,
    VelocidadKmH            FLOAT NOT NULL CONSTRAINT DF_Monitoreo_Vel DEFAULT 0.0,
    Rumbo                   FLOAT NULL,                      -- Grados (0 - 360)
    PrecisionM              FLOAT NOT NULL CONSTRAINT DF_Monitoreo_Prec DEFAULT 5.0,
    BateriaPorcentaje       INT NOT NULL CONSTRAINT DF_Monitoreo_Bat DEFAULT 100,
    UltimaActualizacion     DATETIME2(3) NOT NULL CONSTRAINT DF_Monitoreo_Fecha DEFAULT SYSUTCDATETIME(),
    EnLinea                 BIT NOT NULL CONSTRAINT DF_Monitoreo_Online DEFAULT 1,
    Geometria               GEOMETRY NULL,

    CONSTRAINT PK_MonitoreoTrabajador PRIMARY KEY CLUSTERED (IdMonitoreo),
    CONSTRAINT UQ_Monitoreo_Trabajador UNIQUE (IdTrabajador),
    CONSTRAINT FK_Monitoreo_Trabajador FOREIGN KEY (IdTrabajador) REFERENCES dbo.Trabajadores(IdTrabajador) ON DELETE CASCADE,
    CONSTRAINT FK_Monitoreo_Asignacion FOREIGN KEY (IdAsignacionActiva) REFERENCES dbo.Asignaciones(IdAsignacion) ON DELETE NO ACTION
);
GO

-- ============================================================================
-- 7. PROCEDIMIENTOS ALMACENADOS DEL MÓDULO DE AGUA
-- ============================================================================

-- A. Procedimiento de Estadísticas de Dashboard General
IF OBJECT_ID(N'dbo.usp_ObtenerDashboardAgua', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_ObtenerDashboardAgua;
GO
CREATE PROCEDURE dbo.usp_ObtenerDashboardAgua
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    SELECT
        (SELECT COUNT(*) FROM dbo.ConexionesAgua WHERE Activo = 1) AS TotalConexiones,
        (SELECT COUNT(*) FROM dbo.ConexionesAgua WHERE Activo = 1 AND EstadoConexion = 'Normal') AS ConexionesNormales,
        (SELECT COUNT(*) FROM dbo.ConexionesAgua WHERE Activo = 1 AND EstadoConexion = 'ParaCorte') AS ConexionesParaCorte,
        (SELECT COUNT(*) FROM dbo.ConexionesAgua WHERE Activo = 1 AND EstadoConexion = 'Cortado') AS ConexionesCortadas,
        (SELECT COUNT(*) FROM dbo.ConexionesAgua WHERE Activo = 1 AND EstadoConexion = 'BajaParcial') AS ConexionesBajaParcial,
        (SELECT ISNULL(SUM(SaldoPendiente), 0) FROM dbo.ConexionesAgua WHERE Activo = 1 AND EstadoConexion = 'ParaCorte') AS DeudaTotalCorte,
        (SELECT COUNT(*) FROM dbo.Asignaciones WHERE Estado IN ('Pendiente', 'EnProgreso')) AS AsignacionesActivas,
        (SELECT COUNT(*) FROM dbo.Trabajadores WHERE Estado = 'EnRuta') AS TrabajadoresEnRuta,
        (SELECT COUNT(*) FROM dbo.Trabajadores WHERE Activo = 1) AS TotalTrabajadores;
END;
GO

-- B. Procedimiento de Consulta Espacial de Conexiones de Agua (con filtro por Estado)
IF OBJECT_ID(N'dbo.usp_ObtenerConexionesAgua', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_ObtenerConexionesAgua;
GO
CREATE PROCEDURE dbo.usp_ObtenerConexionesAgua
    @Estado VARCHAR(30) = NULL,             -- 'Normal', 'ParaCorte', 'Cortado', 'BajaParcial' o NULL/'todas'
    @MinLon FLOAT = NULL,
    @MinLat FLOAT = NULL,
    @MaxLon FLOAT = NULL,
    @MaxLat FLOAT = NULL,
    @Busqueda VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    IF @Estado = 'todas' OR @Estado = '' SET @Estado = NULL;
    DECLARE @TerminoLike VARCHAR(105) = CASE WHEN @Busqueda IS NOT NULL THEN '%' + TRIM(@Busqueda) + '%' ELSE NULL END;
    DECLARE @BBox GEOMETRY = NULL;

    IF (@MinLon IS NOT NULL AND @MinLat IS NOT NULL AND @MaxLon IS NOT NULL AND @MaxLat IS NOT NULL)
    BEGIN
        SET @BBox = GEOMETRY::STPolyFromText(
            'POLYGON((' + 
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + ', ' +
            CAST(@MaxLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + ', ' +
            CAST(@MaxLon AS VARCHAR(30)) + ' ' + CAST(@MaxLat AS VARCHAR(30)) + ', ' +
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MaxLat AS VARCHAR(30)) + ', ' +
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + '))', 
            4326
        );
    END;

    SELECT 
        c.IdConexion,
        c.CodigoConexion,
        c.NumeroMedidor,
        c.CodigoLote,
        c.CodigoManzana,
        c.Titular,
        c.Direccion,
        c.Categoria,
        c.EstadoConexion,
        c.MesesMora,
        c.SaldoPendiente,
        c.UltimaLectura,
        c.Longitud,
        c.Latitud,
        c.Geometria.STAsText() AS WKT
    FROM dbo.ConexionesAgua c
    WHERE c.Activo = 1
      AND (@Estado IS NULL OR c.EstadoConexion = @Estado)
      AND (@BBox IS NULL OR c.Geometria.STIntersects(@BBox) = 1)
      AND (@TerminoLike IS NULL OR (c.CodigoConexion LIKE @TerminoLike 
                                OR c.NumeroMedidor LIKE @TerminoLike 
                                OR c.Titular LIKE @TerminoLike 
                                OR c.CodigoLote LIKE @TerminoLike 
                                OR c.Direccion LIKE @TerminoLike))
    ORDER BY c.CodigoConexion ASC;
END;
GO

-- C. Procedimiento para Obtener Conexiones Candidatas para Asignación
IF OBJECT_ID(N'dbo.usp_ObtenerCandidatosAsignacion', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_ObtenerCandidatosAsignacion;
GO
CREATE PROCEDURE dbo.usp_ObtenerCandidatosAsignacion
    @TipoServicio VARCHAR(30),              -- 'Corte' -> Busca 'ParaCorte'; 'Reconexion' -> Busca 'Cortado'
    @Cantidad INT = 10,
    @PuntoPartidaLon FLOAT,
    @PuntoPartidaLat FLOAT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    DECLARE @EstadoObjetivo VARCHAR(30) = CASE WHEN @TipoServicio = 'Corte' THEN 'ParaCorte' ELSE 'Cortado' END;
    DECLARE @PuntoPartida GEOMETRY = GEOMETRY::Point(@PuntoPartidaLon, @PuntoPartidaLat, 4326);

    SELECT TOP (@Cantidad)
        c.IdConexion,
        c.CodigoConexion,
        c.NumeroMedidor,
        c.CodigoLote,
        c.CodigoManzana,
        c.Titular,
        c.Direccion,
        c.EstadoConexion,
        c.MesesMora,
        c.SaldoPendiente,
        c.UltimaLectura,
        c.Longitud,
        c.Latitud,
        c.Geometria.STDistance(@PuntoPartida) * 111139.0 AS DistanciaAproximadaMetros,
        c.Geometria.STAsText() AS WKT
    FROM dbo.ConexionesAgua c
    WHERE c.Activo = 1
      AND c.EstadoConexion = @EstadoObjetivo
      -- Excluir conexiones que ya estén asignadas en una ruta activa pendiente o en progreso
      AND NOT EXISTS (
          SELECT 1 
          FROM dbo.AsignacionDetalle ad
          INNER JOIN dbo.Asignaciones a ON ad.IdAsignacion = a.IdAsignacion
          WHERE ad.IdConexion = c.IdConexion
            AND a.Estado IN ('Pendiente', 'EnProgreso')
      )
    ORDER BY c.Geometria.STDistance(@PuntoPartida) ASC;
END;
GO

-- D. Procedimiento para Registrar Ejecución en Campo (con Validación Obligatoria de GPS)
IF OBJECT_ID(N'dbo.usp_RegistrarEjecucionCampo', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_RegistrarEjecucionCampo;
GO
CREATE PROCEDURE dbo.usp_RegistrarEjecucionCampo
    @IdDetalle INT,
    @EstadoResultado VARCHAR(30),          -- 'Ejecutado', 'CasaCerrada', 'Imposible', 'Rechazado'
    @LecturaActual DECIMAL(10,2) = NULL,
    @NumeroMedidorReportado VARCHAR(50) = NULL,
    @FotoMedidorUrl VARCHAR(500) = NULL,
    @Observacion VARCHAR(500) = NULL,
    @GpsLat FLOAT,
    @GpsLon FLOAT,
    @GpsPrecisionM FLOAT = 5.0,
    @ToleranciaMetrosMax FLOAT = 60.0       -- Tolerancia máxima en metros permitida entre el móvil y el medidor
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @IdConexion INT;
    DECLARE @IdAsignacion INT;
    DECLARE @MedidorGeom GEOMETRY;
    DECLARE @GpsGeom GEOMETRY;
    DECLARE @DistanciaCalculadaM DECIMAL(10,2);
    DECLARE @ValidacionGpsOk BIT;
    DECLARE @TipoServicio VARCHAR(30);

    SELECT 
        @IdConexion = ad.IdConexion,
        @IdAsignacion = ad.IdAsignacion,
        @MedidorGeom = c.Geometria,
        @TipoServicio = a.TipoServicio
    FROM dbo.AsignacionDetalle ad
    INNER JOIN dbo.Asignaciones a ON ad.IdAsignacion = a.IdAsignacion
    INNER JOIN dbo.ConexionesAgua c ON ad.IdConexion = c.IdConexion
    WHERE ad.IdDetalle = @IdDetalle;

    IF @IdConexion IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, 'No se encontró la tarea de asignación especificada.', 1;
        RETURN;
    END;

    -- Construir punto GPS del operario
    SET @GpsGeom = GEOMETRY::Point(@GpsLon, @GpsLat, 4326);

    -- Calcular distancia geodésica aproximada (grados a metros aprox. en latitud media de WGS84)
    -- 1 grado aprox = 111,139 metros
    SET @DistanciaCalculadaM = CAST(@MedidorGeom.STDistance(@GpsGeom) * 111139.0 AS DECIMAL(10,2));

    -- Validación estricta: si está dentro de la tolerancia, es válida; si está más lejos se marca para auditoría
    IF @DistanciaCalculadaM <= @ToleranciaMetrosMax
        SET @ValidacionGpsOk = 1;
    ELSE
        SET @ValidacionGpsOk = 0;

    -- 1. Actualizar el detalle de la asignación
    UPDATE dbo.AsignacionDetalle
    SET 
        Estado = @EstadoResultado,
        LecturaActual = ISNULL(@LecturaActual, LecturaAnterior),
        NumeroMedidorReportado = @NumeroMedidorReportado,
        FotoMedidorUrl = @FotoMedidorUrl,
        Observacion = @Observacion,
        FechaHoraEjecucion = SYSUTCDATETIME(),
        GpsLatEjecucion = @GpsLat,
        GpsLonEjecucion = @GpsLon,
        GpsPrecisionM = @GpsPrecisionM,
        DistanciaValidacionGpsM = @DistanciaCalculadaM,
        ValidacionGpsExitosa = @ValidacionGpsOk
    WHERE IdDetalle = @IdDetalle;

    -- 2. Si el servicio fue efectivamente ejecutado, cambiar el estado de la conexión de agua
    IF @EstadoResultado = 'Ejecutado'
    BEGIN
        IF @TipoServicio = 'Corte'
        BEGIN
            UPDATE dbo.ConexionesAgua
            SET EstadoConexion = 'Cortado',
                UltimaLectura = ISNULL(@LecturaActual, UltimaLectura),
                FechaUltimaLectura = SYSUTCDATETIME()
            WHERE IdConexion = @IdConexion;
        END
        ELSE IF @TipoServicio = 'Reconexion'
        BEGIN
            UPDATE dbo.ConexionesAgua
            SET EstadoConexion = 'Normal',
                MesesMora = 0,
                SaldoPendiente = 0.00,
                UltimaLectura = ISNULL(@LecturaActual, UltimaLectura),
                FechaUltimaLectura = SYSUTCDATETIME()
            WHERE IdConexion = @IdConexion;
        END;
    END;

    -- 3. Comprobar si todas las tareas de la asignación ya fueron procesadas para cerrarla
    IF NOT EXISTS (
        SELECT 1 FROM dbo.AsignacionDetalle 
        WHERE IdAsignacion = @IdAsignacion AND Estado = 'Pendiente'
    )
    BEGIN
        UPDATE dbo.Asignaciones
        SET Estado = 'Completada', FechaFinalizacion = SYSUTCDATETIME()
        WHERE IdAsignacion = @IdAsignacion;

        -- Liberar estado del trabajador
        UPDATE t
        SET t.Estado = 'Disponible'
        FROM dbo.Trabajadores t
        INNER JOIN dbo.Asignaciones a ON t.IdTrabajador = a.IdTrabajador
        WHERE a.IdAsignacion = @IdAsignacion;
    END
    ELSE
    BEGIN
        UPDATE dbo.Asignaciones
        SET Estado = 'EnProgreso'
        WHERE IdAsignacion = @IdAsignacion AND Estado = 'Pendiente';
    END;

    -- 4. Registrar en bitácora de auditoría
    INSERT INTO dbo.BitacoraEventos (Usuario, TipoEvento, Modulo, Descripcion, DetalleTecnico, Exitoso)
    VALUES (
        'Móvil Operario',
        'RegistroCampo',
        'CortesYReconexiones',
        'Tarea #' + CAST(@IdDetalle AS VARCHAR(10)) + ' ' + @EstadoResultado + ' (Distancia GPS: ' + CAST(@DistanciaCalculadaM AS VARCHAR(20)) + 'm)',
        'GpsLat=' + CAST(@GpsLat AS VARCHAR(30)) + ', GpsLon=' + CAST(@GpsLon AS VARCHAR(30)) + ', Validacion=' + CAST(@ValidacionGpsOk AS VARCHAR(5)),
        1
    );

    COMMIT TRANSACTION;

    SELECT 
        @ValidacionGpsOk AS ValidacionGpsExitosa,
        @DistanciaCalculadaM AS DistanciaMetros,
        @DistanciaCalculadaM AS DistanciaMetrosCalculada,
        @EstadoResultado AS EstadoFinal,
        'Registro de campo procesado correctamente.' AS Mensaje;
END;
GO

-- E. Procedimiento para Actualizar Ubicación en Tiempo Real (Monitoreo)
IF OBJECT_ID(N'dbo.usp_ActualizarUbicacionMonitoreo', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_ActualizarUbicacionMonitoreo;
GO
CREATE PROCEDURE dbo.usp_ActualizarUbicacionMonitoreo
    @IdTrabajador INT,
    @Latitud FLOAT,
    @Longitud FLOAT,
    @VelocidadKmH FLOAT = 0.0,
    @Rumbo FLOAT = NULL,
    @PrecisionM FLOAT = 5.0,
    @BateriaPorcentaje INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdAsignacionActiva INT;
    SELECT TOP 1 @IdAsignacionActiva = IdAsignacion
    FROM dbo.Asignaciones
    WHERE IdTrabajador = @IdTrabajador AND Estado = 'EnProgreso'
    ORDER BY IdAsignacion DESC;

    DECLARE @Punto GEOMETRY = GEOMETRY::Point(@Longitud, @Latitud, 4326);

    MERGE dbo.MonitoreoTrabajador AS target
    USING (SELECT @IdTrabajador AS IdTrabajador) AS source
    ON (target.IdTrabajador = source.IdTrabajador)
    WHEN MATCHED THEN
        UPDATE SET 
            IdAsignacionActiva = @IdAsignacionActiva,
            Latitud = @Latitud,
            Longitud = @Longitud,
            VelocidadKmH = @VelocidadKmH,
            Rumbo = @Rumbo,
            PrecisionM = @PrecisionM,
            BateriaPorcentaje = @BateriaPorcentaje,
            UltimaActualizacion = SYSUTCDATETIME(),
            EnLinea = 1,
            Geometria = @Punto
    WHEN NOT MATCHED THEN
        INSERT (IdTrabajador, IdAsignacionActiva, Latitud, Longitud, VelocidadKmH, Rumbo, PrecisionM, BateriaPorcentaje, UltimaActualizacion, EnLinea, Geometria)
        VALUES (@IdTrabajador, @IdAsignacionActiva, @Latitud, @Longitud, @VelocidadKmH, @Rumbo, @PrecisionM, @BateriaPorcentaje, SYSUTCDATETIME(), 1, @Punto);
END;
GO

-- F. Procedimiento para Consultar Operarios en Tiempo Real (Monitoreo Admin)
IF OBJECT_ID(N'dbo.usp_ObtenerMonitoreoOperarios', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_ObtenerMonitoreoOperarios;
GO
CREATE PROCEDURE dbo.usp_ObtenerMonitoreoOperarios
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    SELECT 
        m.IdMonitoreo,
        t.IdTrabajador,
        t.CodigoTrabajador,
        t.NombreCompleto,
        t.Telefono,
        t.TipoTrabajador,
        t.Vehiculo,
        t.Estado AS EstadoOperario,
        m.Latitud,
        m.Longitud,
        m.VelocidadKmH,
        m.Rumbo,
        m.PrecisionM,
        m.BateriaPorcentaje,
        m.UltimaActualizacion,
        m.EnLinea,
        a.CodigoAsignacion,
        a.TipoServicio,
        a.CantidadPuntos,
        (SELECT COUNT(*) FROM dbo.AsignacionDetalle ad WHERE ad.IdAsignacion = a.IdAsignacion AND ad.Estado = 'Ejecutado') AS PuntosCompletados,
        DATEDIFF(SECOND, m.UltimaActualizacion, SYSUTCDATETIME()) AS SegundosDesdeUltimaReporte
    FROM dbo.MonitoreoTrabajador m
    INNER JOIN dbo.Trabajadores t ON m.IdTrabajador = t.IdTrabajador
    LEFT JOIN dbo.Asignaciones a ON m.IdAsignacionActiva = a.IdAsignacion
    WHERE t.Activo = 1
    ORDER BY t.NombreCompleto ASC;
END;
GO

-- ============================================================================
-- 8. POBLACIÓN SEMILLA DE DATOS DEL DOMINIO DE AGUA (WGS 84)
-- ============================================================================
PRINT '>> Poblando datos semilla de Conexiones de Agua, Trabajadores y Asignaciones...';
GO

-- Limpiar si existieran registros previos para re-ejecución idempotente
DELETE FROM dbo.MonitoreoTrabajador;
DELETE FROM dbo.AsignacionDetalle;
DELETE FROM dbo.Asignaciones;
DELETE FROM dbo.ConexionesAgua;
DELETE FROM dbo.Trabajadores;
GO

-- 1. Insertar Trabajadores (Cortadores y Reconectores)
INSERT INTO dbo.Trabajadores (CodigoTrabajador, NombreCompleto, DocumentoIdentidad, Telefono, TipoTrabajador, Estado, Vehiculo, IdUsuario, Activo)
VALUES
    ('TRAB-001', 'Juan Pérez Morales',      '8493021 SC', '77012345', 'Cortador',    'Disponible', 'Moto',     NULL, 1),
    ('TRAB-002', 'Roberto Gómez Tapia',    '7102934 SC', '76098765', 'Cortador',    'Disponible', 'Moto',     NULL, 1),
    ('TRAB-003', 'Marcos Suárez Vaca',     '6192834 SC', '71034567', 'Reconector',  'Disponible', 'Moto',     NULL, 1),
    ('TRAB-004', 'David Rojas Peña',       '5918273 SC', '78045612', 'Reconector',  'Disponible', 'A pie',    NULL, 1);
GO

-- 2. Insertar Conexiones de Agua Potable vinculadas a los lotes reales
-- Estados: 'Normal', 'ParaCorte', 'Cortado', 'BajaParcial'
DECLARE @IdLoteA1 INT = (SELECT TOP 1 IdLote FROM dbo.Lotes WHERE CodigoLote = 'LT-A-01');
DECLARE @IdLoteA2 INT = (SELECT TOP 1 IdLote FROM dbo.Lotes WHERE CodigoLote = 'LT-A-02');
DECLARE @IdLoteA3 INT = (SELECT TOP 1 IdLote FROM dbo.Lotes WHERE CodigoLote = 'LT-A-03');
DECLARE @IdLoteB1 INT = (SELECT TOP 1 IdLote FROM dbo.Lotes WHERE CodigoLote = 'LT-B-01');
DECLARE @IdLoteB2 INT = (SELECT TOP 1 IdLote FROM dbo.Lotes WHERE CodigoLote = 'LT-B-02');
DECLARE @IdLoteB3 INT = (SELECT TOP 1 IdLote FROM dbo.Lotes WHERE CodigoLote = 'LT-B-03');

INSERT INTO dbo.ConexionesAgua 
(CodigoConexion, NumeroMedidor, IdLote, CodigoLote, CodigoManzana, Titular, Direccion, Categoria, EstadoConexion, MesesMora, SaldoPendiente, UltimaLectura, FechaUltimaLectura, Longitud, Latitud, Geometria, Activo)
VALUES
    -- Conexión 1: Para Corte (Mora de 3 meses) - Lote LT-A-01
    ('CON-1001', 'MED-5541', @IdLoteA1, 'LT-A-01', 'MZ-A', 'Carlos Mendoza Ramos',        'Av. Los Libertadores 142', 'Doméstico',  'ParaCorte',   3, 345.50, 482.30, '2026-09-15', -77.03725, -12.09475, GEOMETRY::Point(-77.03725, -12.09475, 4326), 1),

    -- Conexión 2: Normal (Al día) - Lote LT-A-02
    ('CON-1002', 'MED-5542', @IdLoteA2, 'LT-A-02', 'MZ-A', 'Inmobiliaria El Roble S.A.C.', 'Av. Los Libertadores 156', 'Comercial',  'Normal',      0,   0.00, 1250.80, '2026-10-01', -77.03600, -12.09475, GEOMETRY::Point(-77.03600, -12.09475, 4326), 1),

    -- Conexión 3: Cortado (Servicio suspendido) - Lote LT-A-03
    ('CON-1003', 'MED-5543', @IdLoteA3, 'LT-A-03', 'MZ-A', 'Elena Gutiérrez Vega',        'Calle Los Álamos 210',     'Doméstico',  'Cortado',     5, 620.00, 315.00, '2026-08-20', -77.03600, -12.09520, GEOMETRY::Point(-77.03600, -12.09520, 4326), 1),

    -- Conexión 4: Para Corte (Mora de 2 meses) - Lote LT-B-01
    ('CON-1004', 'MED-5544', @IdLoteB1, 'LT-B-01', 'MZ-B', 'Banco Metropolitano',          'Av. El Sol 402',           'Comercial',  'ParaCorte',   2, 890.20, 2450.10, '2026-09-28', -77.03750, -12.09350, GEOMETRY::Point(-77.03750, -12.09350, 4326), 1),

    -- Conexión 5: Normal (Al día) - Lote LT-B-02
    ('CON-1005', 'MED-5545', @IdLoteB2, 'LT-B-02', 'MZ-B', 'Lucía Fernández Silva',       'Av. El Sol 418',           'Doméstico',  'Normal',      0,   0.00, 198.40, '2026-10-05', -77.03650, -12.09350, GEOMETRY::Point(-77.03650, -12.09350, 4326), 1),

    -- Conexión 6: Baja Parcial (Precintado) - Lote LT-B-03
    ('CON-1006', 'MED-5546', @IdLoteB3, 'LT-B-03', 'MZ-B', 'Centro Médico San Isidro',     'Calle Los Pinos 305',      'Equipamiento', 'BajaParcial', 0,   0.00, 89.00, '2026-07-10', -77.03550, -12.09350, GEOMETRY::Point(-77.03550, -12.09350, 4326), 1),

    -- Conexión 7: Para Corte (Mora de 4 meses) - Sector Adicional
    ('CON-1007', 'MED-5547', NULL, 'LT-C-01', 'MZ-C', 'Guillermo Paz Soldán',       'Av. Principal 550',        'Doméstico',  'ParaCorte',   4, 410.00, 620.50, '2026-08-30', -77.03850, -12.09600, GEOMETRY::Point(-77.03850, -12.09600, 4326), 1),

    -- Conexión 8: Cortado (Pendiente de Reconexión) - Sector Adicional
    ('CON-1008', 'MED-5548', NULL, 'LT-C-02', 'MZ-C', 'Farmacia Santa María',       'Av. Principal 562',        'Comercial',  'Cortado',     1, 150.00, 440.00, '2026-10-10', -77.03750, -12.09600, GEOMETRY::Point(-77.03750, -12.09600, 4326), 1),

    -- Conexión 9: Normal (Al día)
    ('CON-1009', 'MED-5549', NULL, 'LT-D-01', 'MZ-D', 'Jorge Valenzuela Prado',     'Pasaje Santa Rosa 12',     'Doméstico',  'Normal',      0,   0.00, 312.20, '2026-10-02', -77.03450, -12.09450, GEOMETRY::Point(-77.03450, -12.09450, 4326), 1),

    -- Conexión 10: Para Corte (Mora de 2 meses)
    ('CON-1010', 'MED-5550', NULL, 'LT-D-02', 'MZ-D', 'Panadería La Espiga',        'Pasaje Santa Rosa 24',     'Comercial',  'ParaCorte',   2, 290.00, 890.00, '2026-09-25', -77.03350, -12.09450, GEOMETRY::Point(-77.03350, -12.09450, 4326), 1);
GO

-- 3. Insertar registro inicial de Monitoreo en Tiempo Real para los operarios
DECLARE @IdT1 INT = (SELECT TOP 1 IdTrabajador FROM dbo.Trabajadores WHERE CodigoTrabajador = 'TRAB-001');
DECLARE @IdT2 INT = (SELECT TOP 1 IdTrabajador FROM dbo.Trabajadores WHERE CodigoTrabajador = 'TRAB-002');
DECLARE @IdT3 INT = (SELECT TOP 1 IdTrabajador FROM dbo.Trabajadores WHERE CodigoTrabajador = 'TRAB-003');
DECLARE @IdT4 INT = (SELECT TOP 1 IdTrabajador FROM dbo.Trabajadores WHERE CodigoTrabajador = 'TRAB-004');

INSERT INTO dbo.MonitoreoTrabajador (IdTrabajador, IdAsignacionActiva, Latitud, Longitud, VelocidadKmH, Rumbo, PrecisionM, BateriaPorcentaje, UltimaActualizacion, EnLinea, Geometria)
VALUES 
    (@IdT1, NULL, -12.09400, -77.03700, 18.5, 90.0, 4.0, 92, SYSUTCDATETIME(), 1, GEOMETRY::Point(-77.03700, -12.09400, 4326)),
    (@IdT2, NULL, -12.09500, -77.03600,  0.0,  0.0, 3.5, 85, SYSUTCDATETIME(), 1, GEOMETRY::Point(-77.03600, -12.09500, 4326)),
    (@IdT3, NULL, -12.09350, -77.03800, 25.0, 180.0, 5.0, 78, SYSUTCDATETIME(), 1, GEOMETRY::Point(-77.03800, -12.09350, 4326)),
    (@IdT4, NULL, -12.09600, -77.03500,  4.5, 270.0, 6.0, 95, SYSUTCDATETIME(), 1, GEOMETRY::Point(-77.03500, -12.09600, 4326));
GO

PRINT '>> [OK] Módulo de Cortes y Reconexiones de Agua Potable desplegado exitosamente.';
GO

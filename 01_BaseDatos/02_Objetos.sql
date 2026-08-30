-- ============================================================================
-- PROYECTO: VisorDatosSIG 2026
-- ARCHIVO: 01_BaseDatos/02_Objetos.sql
-- DESCRIPCIÓN: Tablas espaciales, restricciones, índices espaciales y convencionales,
--              vistas y procedimientos almacenados para las 4 capas y soporte.
-- MOTOR: Microsoft SQL Server 2022
-- SISTEMA DE REFERENCIA ESPACIAL: WGS 84 (SRID 4326)
-- CONVENCIÓN COORDENADAS: geometry (X = Longitud, Y = Latitud)
-- ============================================================================

USE VisorDatosSIG_BD;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- LIMPIEZA PREVIA EN ORDEN CORRECTO DE DEPENDENCIAS (FKs)
-- ============================================================================
IF OBJECT_ID(N'dbo.Lotes', N'U') IS NOT NULL DROP TABLE dbo.Lotes;
IF OBJECT_ID(N'dbo.Manzanas', N'U') IS NOT NULL DROP TABLE dbo.Manzanas;
IF OBJECT_ID(N'dbo.CodigosFijos', N'U') IS NOT NULL DROP TABLE dbo.CodigosFijos;
IF OBJECT_ID(N'dbo.Vias', N'U') IS NOT NULL DROP TABLE dbo.Vias;
IF OBJECT_ID(N'dbo.CatalogoCapas', N'U') IS NOT NULL DROP TABLE dbo.CatalogoCapas;
IF OBJECT_ID(N'dbo.Migraciones', N'U') IS NOT NULL DROP TABLE dbo.Migraciones;
GO

-- ============================================================================
-- 1. TABLA: Migraciones (Trazabilidad e historial de cargas de Shapefiles)
-- ============================================================================
CREATE TABLE dbo.Migraciones (
    IdMigracion         INT IDENTITY(1,1) NOT NULL,
    FechaInicio         DATETIME2(3) NOT NULL CONSTRAINT DF_Migraciones_FechaInicio DEFAULT SYSUTCDATETIME(),
    FechaFin            DATETIME2(3) NULL,
    Usuario             VARCHAR(100) NOT NULL,
    Capa                VARCHAR(50) NOT NULL, -- 'Manzanas', 'Lotes', 'CodigosFijos', 'Vias'
    NombreArchivo       VARCHAR(255) NOT NULL,
    RutaArchivo         VARCHAR(500) NULL,
    Modalidad           VARCHAR(20) NOT NULL, -- 'Reemplazar', 'Anexar'
    TotalRegistros      INT NOT NULL CONSTRAINT DF_Migraciones_Total DEFAULT 0,
    RegistrosExitosos   INT NOT NULL CONSTRAINT DF_Migraciones_Exitosos DEFAULT 0,
    RegistrosOmitidos   INT NOT NULL CONSTRAINT DF_Migraciones_Omitidos DEFAULT 0,
    RegistrosFallidos   INT NOT NULL CONSTRAINT DF_Migraciones_Fallidos DEFAULT 0,
    Estado              VARCHAR(50) NOT NULL, -- 'En Proceso', 'Exitoso', 'Fallido', 'Revertido', 'Cancelado'
    DuracionMs          BIGINT NULL,
    Detalle             NVARCHAR(MAX) NULL, -- Bitácora JSON o texto de advertencias / errores
    CONSTRAINT PK_Migraciones PRIMARY KEY CLUSTERED (IdMigracion)
);
GO

-- ============================================================================
-- 2. TABLA: CatalogoCapas (Configuración cartográfica, estilos y visibilidad)
-- ============================================================================
CREATE TABLE dbo.CatalogoCapas (
    IdCapa              INT IDENTITY(1,1) NOT NULL,
    Nombre              VARCHAR(50) NOT NULL,
    Titulo              VARCHAR(100) NOT NULL,
    Descripcion         VARCHAR(250) NULL,
    TipoGeometria       VARCHAR(50) NOT NULL, -- 'Polygon', 'Point', 'LineString'
    ColorBorde          VARCHAR(20) NOT NULL CONSTRAINT DF_CatalogoCapas_ColorBorde DEFAULT '#3388ff',
    ColorFondo          VARCHAR(20) NOT NULL CONSTRAINT DF_CatalogoCapas_ColorFondo DEFAULT '#3388ff',
    Opacidad            DECIMAL(3,2) NOT NULL CONSTRAINT DF_CatalogoCapas_Opacidad DEFAULT 0.60,
    GrosorBorde         INT NOT NULL CONSTRAINT DF_CatalogoCapas_Grosor DEFAULT 2,
    OrdenVisualizacion  INT NOT NULL CONSTRAINT DF_CatalogoCapas_Orden DEFAULT 1,
    Activo              BIT NOT NULL CONSTRAINT DF_CatalogoCapas_Activo DEFAULT 1,
    Icono               VARCHAR(50) NULL,
    CamposVisibles      NVARCHAR(MAX) NULL, -- JSON con metadatos de atributos para popups/tablas
    CONSTRAINT PK_CatalogoCapas PRIMARY KEY CLUSTERED (IdCapa),
    CONSTRAINT UQ_CatalogoCapas_Nombre UNIQUE (Nombre)
);
GO

-- ============================================================================
-- 3. TABLA ESPACIAL: Manzanas (Contexto Urbano - Polígonos)
-- ============================================================================
CREATE TABLE dbo.Manzanas (
    IdManzana           INT IDENTITY(1,1) NOT NULL,
    CodigoManzana       VARCHAR(50) NOT NULL, -- Clave natural ej. 'MZ-01', '010101'
    Sector              VARCHAR(50) NULL,
    Barrio              VARCHAR(100) NULL,
    AreaM2              DECIMAL(18,4) NULL,
    PerimetroM          DECIMAL(18,4) NULL,
    OrigenId            VARCHAR(100) NULL, -- Trazabilidad SHP (FID / Id externo)
    Geometria           GEOMETRY NOT NULL,
    FechaRegistro       DATETIME2(3) NOT NULL CONSTRAINT DF_Manzanas_FechaRegistro DEFAULT SYSUTCDATETIME(),
    IdMigracion         INT NULL,
    CONSTRAINT PK_Manzanas PRIMARY KEY CLUSTERED (IdManzana),
    CONSTRAINT UQ_Manzanas_CodigoManzana UNIQUE (CodigoManzana),
    CONSTRAINT FK_Manzanas_Migraciones FOREIGN KEY (IdMigracion) REFERENCES dbo.Migraciones(IdMigracion) ON DELETE SET NULL,
    CONSTRAINT CK_Manzanas_SRID CHECK (Geometria.STSrid = 4326),
    CONSTRAINT CK_Manzanas_Valida CHECK (Geometria.STIsValid() = 1)
);
GO

-- ============================================================================
-- 4. TABLA ESPACIAL: Lotes (Consulta Principal y Predial - Polígonos)
-- ============================================================================
CREATE TABLE dbo.Lotes (
    IdLote              INT IDENTITY(1,1) NOT NULL,
    CodigoLote          VARCHAR(50) NOT NULL, -- Clave natural ej. 'LT-0101-001'
    CodigoManzana       VARCHAR(50) NULL,
    NumeroPredial       VARCHAR(50) NULL,
    UsoSuelo            VARCHAR(100) NULL, -- 'Residencial', 'Comercial', 'Equipamiento', 'Industrial'
    AreaTerrenoM2       DECIMAL(18,4) NULL,
    FrenteM             DECIMAL(18,4) NULL,
    FondoM              DECIMAL(18,4) NULL,
    Titular             VARCHAR(200) NULL,
    Direccion           VARCHAR(250) NULL,
    OrigenId            VARCHAR(100) NULL, -- Trazabilidad SHP
    Geometria           GEOMETRY NOT NULL,
    FechaRegistro       DATETIME2(3) NOT NULL CONSTRAINT DF_Lotes_FechaRegistro DEFAULT SYSUTCDATETIME(),
    IdMigracion         INT NULL,
    CONSTRAINT PK_Lotes PRIMARY KEY CLUSTERED (IdLote),
    CONSTRAINT UQ_Lotes_CodigoLote UNIQUE (CodigoLote),
    CONSTRAINT FK_Lotes_Migraciones FOREIGN KEY (IdMigracion) REFERENCES dbo.Migraciones(IdMigracion) ON DELETE SET NULL,
    CONSTRAINT CK_Lotes_SRID CHECK (Geometria.STSrid = 4326),
    CONSTRAINT CK_Lotes_Valida CHECK (Geometria.STIsValid() = 1)
);
GO

-- ============================================================================
-- 5. TABLA ESPACIAL: CodigosFijos (Marcadores y Puntos Catastrales - Puntos)
-- ============================================================================
CREATE TABLE dbo.CodigosFijos (
    IdCodigoFijo        INT IDENTITY(1,1) NOT NULL,
    CodigoFijo          VARCHAR(50) NOT NULL, -- Clave natural ej. 'CF-5001'
    TipoPunto           VARCHAR(100) NULL, -- 'Vértice Geodésico', 'Poste', 'Hito Catastral', 'Cámara'
    Descripcion         VARCHAR(250) NULL,
    Referencia          VARCHAR(250) NULL,
    CotaZ               DECIMAL(10,4) NULL,
    OrigenId            VARCHAR(100) NULL, -- Trazabilidad SHP
    Geometria           GEOMETRY NOT NULL,
    FechaRegistro       DATETIME2(3) NOT NULL CONSTRAINT DF_CodigosFijos_FechaRegistro DEFAULT SYSUTCDATETIME(),
    IdMigracion         INT NULL,
    CONSTRAINT PK_CodigosFijos PRIMARY KEY CLUSTERED (IdCodigoFijo),
    CONSTRAINT UQ_CodigosFijos_CodigoFijo UNIQUE (CodigoFijo),
    CONSTRAINT FK_CodigosFijos_Migraciones FOREIGN KEY (IdMigracion) REFERENCES dbo.Migraciones(IdMigracion) ON DELETE SET NULL,
    CONSTRAINT CK_CodigosFijos_SRID CHECK (Geometria.STSrid = 4326),
    CONSTRAINT CK_CodigosFijos_Valida CHECK (Geometria.STIsValid() = 1)
);
GO

-- ============================================================================
-- 6. TABLA ESPACIAL: Vias (Red Vial e Infraestructura - Líneas)
-- ============================================================================
CREATE TABLE dbo.Vias (
    IdVia               INT IDENTITY(1,1) NOT NULL,
    CodigoVia           VARCHAR(50) NOT NULL, -- Clave natural ej. 'VIA-1001'
    Nombre              VARCHAR(150) NOT NULL,
    TipoVia             VARCHAR(50) NULL, -- 'Avenida', 'Calle', 'Jirón', 'Pasaje', 'Carretera'
    Jerarquia           VARCHAR(50) NULL, -- 'Arteria Principal', 'Colectora', 'Local', 'Peatonal'
    Sentido             VARCHAR(50) NULL, -- 'Doble Sentido', 'Un Sentido'
    AnchoViaM           DECIMAL(10,2) NULL,
    Pavimento           VARCHAR(50) NULL, -- 'Asfalto', 'Concreto', 'Adoquín', 'Tierra'
    LongitudM           DECIMAL(18,4) NULL,
    OrigenId            VARCHAR(100) NULL, -- Trazabilidad SHP
    Geometria           GEOMETRY NOT NULL,
    FechaRegistro       DATETIME2(3) NOT NULL CONSTRAINT DF_Vias_FechaRegistro DEFAULT SYSUTCDATETIME(),
    IdMigracion         INT NULL,
    CONSTRAINT PK_Vias PRIMARY KEY CLUSTERED (IdVia),
    CONSTRAINT UQ_Vias_CodigoVia UNIQUE (CodigoVia),
    CONSTRAINT FK_Vias_Migraciones FOREIGN KEY (IdMigracion) REFERENCES dbo.Migraciones(IdMigracion) ON DELETE SET NULL,
    CONSTRAINT CK_Vias_SRID CHECK (Geometria.STSrid = 4326),
    CONSTRAINT CK_Vias_Valida CHECK (Geometria.STIsValid() = 1)
);
GO

-- ============================================================================
-- 7. ÍNDICES ESPACIALES Y CONVENCIONALES
-- ============================================================================

-- Índices B-Tree para optimización de filtros alfanuméricos y búsquedas
CREATE NONCLUSTERED INDEX IX_Manzanas_Sector ON dbo.Manzanas (Sector);
CREATE NONCLUSTERED INDEX IX_Manzanas_Barrio ON dbo.Manzanas (Barrio);

CREATE NONCLUSTERED INDEX IX_Lotes_CodigoManzana ON dbo.Lotes (CodigoManzana);
CREATE NONCLUSTERED INDEX IX_Lotes_Titular ON dbo.Lotes (Titular);
CREATE NONCLUSTERED INDEX IX_Lotes_Direccion ON dbo.Lotes (Direccion);
CREATE NONCLUSTERED INDEX IX_Lotes_UsoSuelo ON dbo.Lotes (UsoSuelo);

CREATE NONCLUSTERED INDEX IX_CodigosFijos_TipoPunto ON dbo.CodigosFijos (TipoPunto);

CREATE NONCLUSTERED INDEX IX_Vias_Nombre ON dbo.Vias (Nombre);
CREATE NONCLUSTERED INDEX IX_Vias_TipoVia ON dbo.Vias (TipoVia);
CREATE NONCLUSTERED INDEX IX_Vias_Jerarquia ON dbo.Vias (Jerarquia);

CREATE NONCLUSTERED INDEX IX_Migraciones_FechaInicio ON dbo.Migraciones (FechaInicio DESC);
CREATE NONCLUSTERED INDEX IX_Migraciones_Capa ON dbo.Migraciones (Capa);
GO

-- Índices Espaciales para soporte de consultas con Bounding Box (WGS84 mundial)
-- Coordenadas globales: Longitud [-180, 180], Latitud [-90, 90]
CREATE SPATIAL INDEX SPDX_Manzanas_Geometria ON dbo.Manzanas (Geometria)
WITH (
    BOUNDING_BOX = (XMIN = -180, YMIN = -90, XMAX = 180, YMAX = 90),
    GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    CELLS_PER_OBJECT = 16
);
GO

CREATE SPATIAL INDEX SPDX_Lotes_Geometria ON dbo.Lotes (Geometria)
WITH (
    BOUNDING_BOX = (XMIN = -180, YMIN = -90, XMAX = 180, YMAX = 90),
    GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    CELLS_PER_OBJECT = 16
);
GO

CREATE SPATIAL INDEX SPDX_CodigosFijos_Geometria ON dbo.CodigosFijos (Geometria)
WITH (
    BOUNDING_BOX = (XMIN = -180, YMIN = -90, XMAX = 180, YMAX = 90),
    GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    CELLS_PER_OBJECT = 16
);
GO

CREATE SPATIAL INDEX SPDX_Vias_Geometria ON dbo.Vias (Geometria)
WITH (
    BOUNDING_BOX = (XMIN = -180, YMIN = -90, XMAX = 180, YMAX = 90),
    GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    CELLS_PER_OBJECT = 16
);
GO

-- ============================================================================
-- 8. PROCEDIMIENTOS ALMACENADOS
-- ============================================================================

-- 8.1 Catálogo de capas
IF OBJECT_ID(N'dbo.usp_ObtenerCapasCatalogo', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_ObtenerCapasCatalogo;
GO

CREATE PROCEDURE dbo.usp_ObtenerCapasCatalogo
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
    SELECT 
        IdCapa,
        Nombre,
        Titulo,
        Descripcion,
        TipoGeometria,
        ColorBorde,
        ColorFondo,
        Opacidad,
        GrosorBorde,
        OrdenVisualizacion,
        Activo,
        Icono,
        CamposVisibles
    FROM dbo.CatalogoCapas
    WHERE Activo = 1
    ORDER BY OrdenVisualizacion ASC;
END
GO

-- 8.2 Consulta espacial Manzanas por Bounding Box (o todas si bbox no se provee)
IF OBJECT_ID(N'dbo.usp_ConsultarManzanasPorBBox', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_ConsultarManzanasPorBBox;
GO

CREATE PROCEDURE dbo.usp_ConsultarManzanasPorBBox
    @MinLon FLOAT = NULL,
    @MinLat FLOAT = NULL,
    @MaxLon FLOAT = NULL,
    @MaxLat FLOAT = NULL,
    @Limite INT = 5000
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
    
    IF @MinLon IS NOT NULL AND @MinLat IS NOT NULL AND @MaxLon IS NOT NULL AND @MaxLat IS NOT NULL
    BEGIN
        DECLARE @BBox GEOMETRY = geometry::STGeomFromText(
            'POLYGON((' + 
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + ', ' +
            CAST(@MaxLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + ', ' +
            CAST(@MaxLon AS VARCHAR(30)) + ' ' + CAST(@MaxLat AS VARCHAR(30)) + ', ' +
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MaxLat AS VARCHAR(30)) + ', ' +
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + '))', 4326);

        SELECT TOP (@Limite)
            m.IdManzana,
            m.CodigoManzana,
            m.Sector,
            m.Barrio,
            m.AreaM2,
            m.PerimetroM,
            m.OrigenId,
            m.Geometria.STAsText() AS WKT,
            NULL AS LonCentro,
            NULL AS LatCentro
        FROM dbo.Manzanas m
        WHERE m.Geometria.Filter(@BBox) = 1;
    END
    ELSE
    BEGIN
        SELECT TOP (@Limite)
            m.IdManzana,
            m.CodigoManzana,
            m.Sector,
            m.Barrio,
            m.AreaM2,
            m.PerimetroM,
            m.OrigenId,
            m.Geometria.STAsText() AS WKT,
            NULL AS LonCentro,
            NULL AS LatCentro
        FROM dbo.Manzanas m;
    END
END
GO

-- 8.3 Consulta espacial Lotes por Bounding Box
IF OBJECT_ID(N'dbo.usp_ConsultarLotesPorBBox', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_ConsultarLotesPorBBox;
GO

CREATE PROCEDURE dbo.usp_ConsultarLotesPorBBox
    @MinLon FLOAT = NULL,
    @MinLat FLOAT = NULL,
    @MaxLon FLOAT = NULL,
    @MaxLat FLOAT = NULL,
    @Limite INT = 5000
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
    
    IF @MinLon IS NOT NULL AND @MinLat IS NOT NULL AND @MaxLon IS NOT NULL AND @MaxLat IS NOT NULL
    BEGIN
        DECLARE @BBox GEOMETRY = geometry::STGeomFromText(
            'POLYGON((' + 
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + ', ' +
            CAST(@MaxLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + ', ' +
            CAST(@MaxLon AS VARCHAR(30)) + ' ' + CAST(@MaxLat AS VARCHAR(30)) + ', ' +
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MaxLat AS VARCHAR(30)) + ', ' +
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + '))', 4326);

        SELECT TOP (@Limite)
            l.IdLote,
            l.CodigoLote,
            l.CodigoManzana,
            l.NumeroPredial,
            l.UsoSuelo,
            l.AreaTerrenoM2,
            l.FrenteM,
            l.FondoM,
            l.Titular,
            l.Direccion,
            l.OrigenId,
            l.Geometria.STAsText() AS WKT,
            NULL AS LonCentro,
            NULL AS LatCentro
        FROM dbo.Lotes l
        WHERE l.Geometria.Filter(@BBox) = 1;
    END
    ELSE
    BEGIN
        SELECT TOP (@Limite)
            l.IdLote,
            l.CodigoLote,
            l.CodigoManzana,
            l.NumeroPredial,
            l.UsoSuelo,
            l.AreaTerrenoM2,
            l.FrenteM,
            l.FondoM,
            l.Titular,
            l.Direccion,
            l.OrigenId,
            l.Geometria.STAsText() AS WKT,
            NULL AS LonCentro,
            NULL AS LatCentro
        FROM dbo.Lotes l;
    END
END
GO

-- 8.4 Consulta espacial CodigosFijos por Bounding Box
IF OBJECT_ID(N'dbo.usp_ConsultarCodigosFijosPorBBox', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_ConsultarCodigosFijosPorBBox;
GO

CREATE PROCEDURE dbo.usp_ConsultarCodigosFijosPorBBox
    @MinLon FLOAT = NULL,
    @MinLat FLOAT = NULL,
    @MaxLon FLOAT = NULL,
    @MaxLat FLOAT = NULL,
    @Limite INT = 5000
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
    
    IF @MinLon IS NOT NULL AND @MinLat IS NOT NULL AND @MaxLon IS NOT NULL AND @MaxLat IS NOT NULL
    BEGIN
        DECLARE @BBox GEOMETRY = geometry::STGeomFromText(
            'POLYGON((' + 
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + ', ' +
            CAST(@MaxLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + ', ' +
            CAST(@MaxLon AS VARCHAR(30)) + ' ' + CAST(@MaxLat AS VARCHAR(30)) + ', ' +
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MaxLat AS VARCHAR(30)) + ', ' +
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + '))', 4326);

        SELECT TOP (@Limite)
            c.IdCodigoFijo,
            c.CodigoFijo,
            c.TipoPunto,
            c.Descripcion,
            c.Referencia,
            c.CotaZ,
            c.OrigenId,
            c.Geometria.STAsText() AS WKT,
            c.Geometria.STX AS LonCentro,
            c.Geometria.STY AS LatCentro
        FROM dbo.CodigosFijos c
        WHERE c.Geometria.Filter(@BBox) = 1;
    END
    ELSE
    BEGIN
        SELECT TOP (@Limite)
            c.IdCodigoFijo,
            c.CodigoFijo,
            c.TipoPunto,
            c.Descripcion,
            c.Referencia,
            c.CotaZ,
            c.OrigenId,
            c.Geometria.STAsText() AS WKT,
            c.Geometria.STX AS LonCentro,
            c.Geometria.STY AS LatCentro
        FROM dbo.CodigosFijos c;
    END
END
GO

-- 8.5 Consulta espacial Vias por Bounding Box
IF OBJECT_ID(N'dbo.usp_ConsultarViasPorBBox', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_ConsultarViasPorBBox;
GO

CREATE PROCEDURE dbo.usp_ConsultarViasPorBBox
    @MinLon FLOAT = NULL,
    @MinLat FLOAT = NULL,
    @MaxLon FLOAT = NULL,
    @MaxLat FLOAT = NULL,
    @Limite INT = 5000
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
    
    IF @MinLon IS NOT NULL AND @MinLat IS NOT NULL AND @MaxLon IS NOT NULL AND @MaxLat IS NOT NULL
    BEGIN
        DECLARE @BBox GEOMETRY = geometry::STGeomFromText(
            'POLYGON((' + 
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + ', ' +
            CAST(@MaxLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + ', ' +
            CAST(@MaxLon AS VARCHAR(30)) + ' ' + CAST(@MaxLat AS VARCHAR(30)) + ', ' +
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MaxLat AS VARCHAR(30)) + ', ' +
            CAST(@MinLon AS VARCHAR(30)) + ' ' + CAST(@MinLat AS VARCHAR(30)) + '))', 4326);

        SELECT TOP (@Limite)
            v.IdVia,
            v.CodigoVia,
            v.Nombre,
            v.TipoVia,
            v.Jerarquia,
            v.Sentido,
            v.AnchoViaM,
            v.Pavimento,
            v.LongitudM,
            v.OrigenId,
            v.Geometria.STAsText() AS WKT,
            NULL AS LonCentro,
            NULL AS LatCentro
        FROM dbo.Vias v
        WHERE v.Geometria.Filter(@BBox) = 1;
    END
    ELSE
    BEGIN
        SELECT TOP (@Limite)
            v.IdVia,
            v.CodigoVia,
            v.Nombre,
            v.TipoVia,
            v.Jerarquia,
            v.Sentido,
            v.AnchoViaM,
            v.Pavimento,
            v.LongitudM,
            v.OrigenId,
            v.Geometria.STAsText() AS WKT,
            NULL AS LonCentro,
            NULL AS LatCentro
        FROM dbo.Vias v;
    END
END
GO

-- 8.6 Búsqueda General Unificada (RF-CON-03 y 04)
IF OBJECT_ID(N'dbo.usp_BusquedaGeneral', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_BusquedaGeneral;
GO

CREATE PROCEDURE dbo.usp_BusquedaGeneral
    @Termino VARCHAR(100),
    @Capa VARCHAR(50) = NULL,
    @Pagina INT = 1,
    @RegistrosPorPagina INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
    
    DECLARE @Offset INT = (@Pagina - 1) * @RegistrosPorPagina;
    DECLARE @TerminoLike VARCHAR(105) = '%' + TRIM(@Termino) + '%';

    ;WITH ResultadosBusqueda AS (
        -- Lotes
        SELECT 
            'Lotes' AS Capa,
            l.IdLote AS IdEntidad,
            l.CodigoLote AS Codigo,
            l.Direccion + ' (Titular: ' + ISNULL(l.Titular, 'S/T') + ')' AS Descripcion,
            'Manzana: ' + ISNULL(l.CodigoManzana, '-') + ' | Uso: ' + ISNULL(l.UsoSuelo, '-') AS Detalle,
            l.Geometria.STAsText() AS WKT,
            l.Geometria.STCentroid().STX AS LonCentro,
            l.Geometria.STCentroid().STY AS LatCentro
        FROM dbo.Lotes l
        WHERE (@Capa IS NULL OR @Capa = 'Lotes')
          AND (l.CodigoLote LIKE @TerminoLike 
               OR l.CodigoManzana LIKE @TerminoLike 
               OR l.Titular LIKE @TerminoLike 
               OR l.Direccion LIKE @TerminoLike
               OR l.NumeroPredial LIKE @TerminoLike)
        
        UNION ALL
        
        -- Manzanas
        SELECT 
            'Manzanas' AS Capa,
            m.IdManzana AS IdEntidad,
            m.CodigoManzana AS Codigo,
            'Sector: ' + ISNULL(m.Sector, '-') + ' - Barrio: ' + ISNULL(m.Barrio, '-') AS Descripcion,
            'Área: ' + CAST(ISNULL(m.AreaM2, 0) AS VARCHAR(30)) + ' m²' AS Detalle,
            m.Geometria.STAsText() AS WKT,
            m.Geometria.STCentroid().STX AS LonCentro,
            m.Geometria.STCentroid().STY AS LatCentro
        FROM dbo.Manzanas m
        WHERE (@Capa IS NULL OR @Capa = 'Manzanas')
          AND (m.CodigoManzana LIKE @TerminoLike 
               OR m.Sector LIKE @TerminoLike 
               OR m.Barrio LIKE @TerminoLike)

        UNION ALL
        
        -- CodigosFijos
        SELECT 
            'CodigosFijos' AS Capa,
            c.IdCodigoFijo AS IdEntidad,
            c.CodigoFijo AS Codigo,
            c.TipoPunto + ' - ' + ISNULL(c.Descripcion, '') AS Descripcion,
            'Ref: ' + ISNULL(c.Referencia, '-') + ' | Cota: ' + CAST(ISNULL(c.CotaZ, 0) AS VARCHAR(30)) AS Detalle,
            c.Geometria.STAsText() AS WKT,
            c.Geometria.STX AS LonCentro,
            c.Geometria.STY AS LatCentro
        FROM dbo.CodigosFijos c
        WHERE (@Capa IS NULL OR @Capa = 'CodigosFijos')
          AND (c.CodigoFijo LIKE @TerminoLike 
               OR c.TipoPunto LIKE @TerminoLike 
               OR c.Descripcion LIKE @TerminoLike
               OR c.Referencia LIKE @TerminoLike)

        UNION ALL
        
        -- Vias
        SELECT 
            'Vias' AS Capa,
            v.IdVia AS IdEntidad,
            v.CodigoVia AS Codigo,
            v.TipoVia + ' ' + v.Nombre AS Descripcion,
            'Jerarquía: ' + ISNULL(v.Jerarquia, '-') + ' | Pavimento: ' + ISNULL(v.Pavimento, '-') AS Detalle,
            v.Geometria.STAsText() AS WKT,
            v.Geometria.STCentroid().STX AS LonCentro,
            v.Geometria.STCentroid().STY AS LatCentro
        FROM dbo.Vias v
        WHERE (@Capa IS NULL OR @Capa = 'Vias')
          AND (v.CodigoVia LIKE @TerminoLike 
               OR v.Nombre LIKE @TerminoLike 
               OR v.TipoVia LIKE @TerminoLike)
    )
    SELECT 
        COUNT(*) OVER() AS TotalRegistros,
        Capa,
        IdEntidad,
        Codigo,
        Descripcion,
        Detalle,
        WKT,
        LonCentro,
        LatCentro
    FROM ResultadosBusqueda
    ORDER BY Capa, Codigo
    OFFSET @Offset ROWS
    FETCH NEXT @RegistrosPorPagina ROWS ONLY;
END
GO

-- 8.7 Identificación puntual de entidad por Clic / Bounding Point (RF-CON-01)
IF OBJECT_ID(N'dbo.usp_IdentificarEntidadesPorPunto', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_IdentificarEntidadesPorPunto;
GO

CREATE PROCEDURE dbo.usp_IdentificarEntidadesPorPunto
    @Lon FLOAT,
    @Lat FLOAT,
    @ToleranciaMetros FLOAT = 5.0 -- Tolerancia para puntos y líneas
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Punto GEOMETRY = geometry::Point(@Lon, @Lat, 4326);
    -- Buffer aproximado en grados decimales (1 grado ~ 111,000 m en el ecuador)
    DECLARE @BufferGrados FLOAT = @ToleranciaMetros / 111000.0;
    DECLARE @AreaBuffer GEOMETRY = @Punto.STBuffer(@BufferGrados);

    -- Lotes que contienen el punto
    SELECT 
        'Lotes' AS Capa,
        l.IdLote AS IdEntidad,
        l.CodigoLote AS Codigo,
        l.Titular AS Titulo,
        l.Direccion AS Subtitulo,
        'Uso: ' + ISNULL(l.UsoSuelo, 'N/A') + ', Área: ' + CAST(ISNULL(l.AreaTerrenoM2, 0) AS VARCHAR(20)) + ' m²' AS Resumen,
        l.Geometria.STAsText() AS WKT
    FROM dbo.Lotes l
    WHERE l.Geometria.STContains(@Punto) = 1 OR l.Geometria.STIntersects(@AreaBuffer) = 1

    UNION ALL

    -- Manzanas que contienen el punto
    SELECT 
        'Manzanas' AS Capa,
        m.IdManzana AS IdEntidad,
        m.CodigoManzana AS Codigo,
        'Manzana ' + m.CodigoManzana AS Titulo,
        'Sector: ' + ISNULL(m.Sector, 'N/A') + ', Barrio: ' + ISNULL(m.Barrio, 'N/A') AS Subtitulo,
        'Área: ' + CAST(ISNULL(m.AreaM2, 0) AS VARCHAR(20)) + ' m²' AS Resumen,
        m.Geometria.STAsText() AS WKT
    FROM dbo.Manzanas m
    WHERE m.Geometria.STContains(@Punto) = 1

    UNION ALL

    -- Códigos Fijos cercanos
    SELECT 
        'CodigosFijos' AS Capa,
        c.IdCodigoFijo AS IdEntidad,
        c.CodigoFijo AS Codigo,
        c.TipoPunto AS Titulo,
        c.Descripcion AS Subtitulo,
        'Cota: ' + CAST(ISNULL(c.CotaZ, 0) AS VARCHAR(20)) + ' m' AS Resumen,
        c.Geometria.STAsText() AS WKT
    FROM dbo.CodigosFijos c
    WHERE c.Geometria.STIntersects(@AreaBuffer) = 1

    UNION ALL

    -- Vías intersectadas
    SELECT 
        'Vias' AS Capa,
        v.IdVia AS IdEntidad,
        v.CodigoVia AS Codigo,
        v.TipoVia + ' ' + v.Nombre AS Titulo,
        'Jerarquía: ' + ISNULL(v.Jerarquia, 'N/A') + ', Sentido: ' + ISNULL(v.Sentido, 'N/A') AS Subtitulo,
        'Pavimento: ' + ISNULL(v.Pavimento, 'N/A') + ', Ancho: ' + CAST(ISNULL(v.AnchoViaM, 0) AS VARCHAR(20)) + ' m' AS Resumen,
        v.Geometria.STAsText() AS WKT
    FROM dbo.Vias v
    WHERE v.Geometria.STIntersects(@AreaBuffer) = 1;
END
GO

-- 8.8 Gestión de Migraciones (RF-MIG-10)
IF OBJECT_ID(N'dbo.usp_RegistrarMigracion', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_RegistrarMigracion;
GO

CREATE PROCEDURE dbo.usp_RegistrarMigracion
    @Usuario VARCHAR(100),
    @Capa VARCHAR(50),
    @NombreArchivo VARCHAR(255),
    @RutaArchivo VARCHAR(500),
    @Modalidad VARCHAR(20),
    @TotalRegistros INT,
    @IdMigracion INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO dbo.Migraciones (
        FechaInicio,
        Usuario,
        Capa,
        NombreArchivo,
        RutaArchivo,
        Modalidad,
        TotalRegistros,
        RegistrosExitosos,
        RegistrosOmitidos,
        RegistrosFallidos,
        Estado
    )
    VALUES (
        SYSUTCDATETIME(),
        @Usuario,
        @Capa,
        @NombreArchivo,
        @RutaArchivo,
        @Modalidad,
        @TotalRegistros,
        0,
        0,
        0,
        'En Proceso'
    );

    SET @IdMigracion = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID(N'dbo.usp_ActualizarEstadoMigracion', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_ActualizarEstadoMigracion;
GO

CREATE PROCEDURE dbo.usp_ActualizarEstadoMigracion
    @IdMigracion INT,
    @RegistrosExitosos INT,
    @RegistrosOmitidos INT,
    @RegistrosFallidos INT,
    @Estado VARCHAR(50),
    @Detalle NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @FechaInicio DATETIME2(3);
    SELECT @FechaInicio = FechaInicio FROM dbo.Migraciones WHERE IdMigracion = @IdMigracion;

    UPDATE dbo.Migraciones
    SET 
        FechaFin = SYSUTCDATETIME(),
        RegistrosExitosos = @RegistrosExitosos,
        RegistrosOmitidos = @RegistrosOmitidos,
        RegistrosFallidos = @RegistrosFallidos,
        Estado = @Estado,
        DuracionMs = DATEDIFF(MILLISECOND, @FechaInicio, SYSUTCDATETIME()),
        Detalle = @Detalle
    WHERE IdMigracion = @IdMigracion;
END
GO

PRINT '>> Tablas espaciales, restricciones, índices y procedimientos almacenados creados exitosamente.';
GO

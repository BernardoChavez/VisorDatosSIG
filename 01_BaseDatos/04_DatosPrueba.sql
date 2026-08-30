-- ============================================================================
-- PROYECTO: VisorDatosSIG 2026
-- ARCHIVO: 01_BaseDatos/04_DatosPrueba.sql
-- DESCRIPCIÓN: Carga de catálogo de capas con simbología cartográfica, registro
--              semilla de migración y datos espaciales reales en WGS 84 (SRID 4326).
-- MOTOR: Microsoft SQL Server 2022
-- SISTEMA DE REFERENCIA: EPSG:4326 (WGS 84 - Coordenadas X=Longitud, Y=Latitud)
-- ============================================================================

USE VisorDatosSIG_BD;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- 1. POBLAR CATÁLOGO DE CAPAS Y ESTILOS CARTOGRÁFICOS
-- ============================================================================
DELETE FROM dbo.CatalogoCapas;
GO

SET IDENTITY_INSERT dbo.CatalogoCapas ON;

INSERT INTO dbo.CatalogoCapas (
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
)
VALUES 
    (
        1,
        'Manzanas',
        'Manzanas Urbanas',
        'Delimitación física de manzanas del tejido urbano y catastral.',
        'Polygon',
        '#d35400',
        '#e67e22',
        0.35,
        2,
        1,
        1,
        'bi-grid-3x3',
        '[{"campo":"CodigoManzana","alias":"Cód. Manzana"},{"campo":"Sector","alias":"Sector"},{"campo":"Barrio","alias":"Barrio"},{"campo":"AreaM2","alias":"Área (m²)"},{"campo":"PerimetroM","alias":"Perímetro (m)"}]'
    ),
    (
        2,
        'Lotes',
        'Lotes Prediales',
        'Predios urbanos catastrados con información de uso, titular y área.',
        'Polygon',
        '#1f77b4',
        '#3498db',
        0.55,
        2,
        2,
        1,
        'bi-houses',
        '[{"campo":"CodigoLote","alias":"Cód. Lote"},{"campo":"CodigoManzana","alias":"Manzana"},{"campo":"NumeroPredial","alias":"Nº Predial"},{"campo":"UsoSuelo","alias":"Uso del Suelo"},{"campo":"Titular","alias":"Titular"},{"campo":"Direccion","alias":"Dirección"},{"campo":"AreaTerrenoM2","alias":"Área Terreno (m²)"}]'
    ),
    (
        3,
        'CodigosFijos',
        'Códigos Fijos e Hitos',
        'Puntos de control geodésico, hitos catastrales y vértices de referencia.',
        'Point',
        '#c0392b',
        '#e74c3c',
        0.95,
        3,
        3,
        1,
        'bi-geo-alt-fill',
        '[{"campo":"CodigoFijo","alias":"Cód. Fijo"},{"campo":"TipoPunto","alias":"Tipo de Hito"},{"campo":"Descripcion","alias":"Descripción"},{"campo":"Referencia","alias":"Referencia"},{"campo":"CotaZ","alias":"Cota Z (m.s.n.m.)"}]'
    ),
    (
        4,
        'Vias',
        'Red Vial y Ejes Viales',
        'Trazado de avenidas, calles, jirones y pasajes con datos de pavimento y jerarquía.',
        'LineString',
        '#2c3e50',
        '#34495e',
        0.85,
        3,
        4,
        1,
        'bi-signpost-split',
        '[{"campo":"CodigoVia","alias":"Cód. Vía"},{"campo":"Nombre","alias":"Nombre de Vía"},{"campo":"TipoVia","alias":"Tipo de Vía"},{"campo":"Jerarquia","alias":"Jerarquía"},{"campo":"Sentido","alias":"Sentido de Tránsito"},{"campo":"Pavimento","alias":"Pavimento"},{"campo":"AnchoViaM","alias":"Ancho (m)"},{"campo":"LongitudM","alias":"Longitud (m)"}]'
    );

SET IDENTITY_INSERT dbo.CatalogoCapas OFF;
GO

-- ============================================================================
-- 2. REGISTRO SEMILLA DE MIGRACIÓN (TRAZABILIDAD)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Migraciones WHERE IdMigracion = 1)
BEGIN
    SET IDENTITY_INSERT dbo.Migraciones ON;

    INSERT INTO dbo.Migraciones (
        IdMigracion,
        FechaInicio,
        FechaFin,
        Usuario,
        Capa,
        NombreArchivo,
        RutaArchivo,
        Modalidad,
        TotalRegistros,
        RegistrosExitosos,
        RegistrosOmitidos,
        RegistrosFallidos,
        Estado,
        DuracionMs,
        Detalle
    )
    VALUES 
        (
            1,
            DATEADD(MINUTE, -30, SYSUTCDATETIME()),
            DATEADD(MINUTE, -28, SYSUTCDATETIME()),
            'admin',
            'Carga Inicial del Sistema',
            'Dataset_Catastro_Semilla_2026.zip',
            'C:\SIG\Datasets\Dataset_Catastro_Semilla_2026.zip',
            'Reemplazar',
            30,
            30,
            0,
            0,
            'Exitoso',
            124500,
            '{"mensaje":"Carga inicial de datos de prueba exitosa para las 4 capas espaciales (Manzanas, Lotes, Códigos Fijos y Vías) en proyección WGS84."}'
        );

    SET IDENTITY_INSERT dbo.Migraciones OFF;
END
GO

-- ============================================================================
-- 3. POBLAR CAPA: MANZANAS (POLÍGONOS EN WGS 84)
-- Coordenadas centradas en zona urbana representativa (-77.035, -12.095)
-- ============================================================================
DELETE FROM dbo.Manzanas;
GO

INSERT INTO dbo.Manzanas (CodigoManzana, Sector, Barrio, AreaM2, PerimetroM, OrigenId, Geometria, IdMigracion)
VALUES
-- Manzana A (Noroeste)
(
    'MZ-A',
    'Sector 01',
    'San Isidro Centro',
    12500.50,
    450.20,
    'SHP_MZ_001',
    geometry::STGeomFromText('POLYGON((-77.0380 -12.0940, -77.0350 -12.0940, -77.0350 -12.0970, -77.0380 -12.0970, -77.0380 -12.0940))', 4326),
    1
),
-- Manzana B (Noreste)
(
    'MZ-B',
    'Sector 01',
    'San Isidro Centro',
    13200.00,
    465.80,
    'SHP_MZ_002',
    geometry::STGeomFromText('POLYGON((-77.0340 -12.0940, -77.0310 -12.0940, -77.0310 -12.0970, -77.0340 -12.0970, -77.0340 -12.0940))', 4326),
    1
),
-- Manzana C (Suroeste)
(
    'MZ-C',
    'Sector 02',
    'Las Flores',
    11800.75,
    438.40,
    'SHP_MZ_003',
    geometry::STGeomFromText('POLYGON((-77.0380 -12.0980, -77.0350 -12.0980, -77.0350 -12.1010, -77.0380 -12.1010, -77.0380 -12.0980))', 4326),
    1
),
-- Manzana D (Sureste)
(
    'MZ-D',
    'Sector 02',
    'Las Flores',
    12950.20,
    458.10,
    'SHP_MZ_004',
    geometry::STGeomFromText('POLYGON((-77.0340 -12.0980, -77.0310 -12.0980, -77.0310 -12.1010, -77.0340 -12.1010, -77.0340 -12.0980))', 4326),
    1
);
GO

-- ============================================================================
-- 4. POBLAR CAPA: LOTES (POLÍGONOS PREDIALES EN WGS 84)
-- Subdivididos dentro de las manzanas anteriores
-- ============================================================================
DELETE FROM dbo.Lotes;
GO

INSERT INTO dbo.Lotes (CodigoLote, CodigoManzana, NumeroPredial, UsoSuelo, AreaTerrenoM2, FrenteM, FondoM, Titular, Direccion, OrigenId, Geometria, IdMigracion)
VALUES
-- Lotes en Manzana A
(
    'LT-A-01',
    'MZ-A',
    'P-101',
    'Residencial R3',
    4100.00,
    15.00,
    25.00,
    'Carlos Mendoza Ramos',
    'Av. Los Libertadores 142',
    'SHP_LT_001',
    geometry::STGeomFromText('POLYGON((-77.0380 -12.0940, -77.0365 -12.0940, -77.0365 -12.0955, -77.0380 -12.0955, -77.0380 -12.0940))', 4326),
    1
),
(
    'LT-A-02',
    'MZ-A',
    'P-102',
    'Comercial C2',
    4100.00,
    15.00,
    25.00,
    'Inmobiliaria El Roble S.A.C.',
    'Av. Los Libertadores 156',
    'SHP_LT_002',
    geometry::STGeomFromText('POLYGON((-77.0365 -12.0940, -77.0350 -12.0940, -77.0350 -12.0955, -77.0365 -12.0955, -77.0365 -12.0940))', 4326),
    1
),
(
    'LT-A-03',
    'MZ-A',
    'P-103',
    'Residencial R3',
    4300.50,
    20.00,
    25.00,
    'Elena Gutiérrez Vega',
    'Calle Los Álamos 210',
    'SHP_LT_003',
    geometry::STGeomFromText('POLYGON((-77.0380 -12.0955, -77.0350 -12.0955, -77.0350 -12.0970, -77.0380 -12.0970, -77.0380 -12.0955))', 4326),
    1
),

-- Lotes en Manzana B
(
    'LT-B-01',
    'MZ-B',
    'P-201',
    'Comercial C3',
    4400.00,
    18.00,
    28.00,
    'Banco Metropolitano Financiero',
    'Av. El Sol 402',
    'SHP_LT_004',
    geometry::STGeomFromText('POLYGON((-77.0340 -12.0940, -77.0325 -12.0940, -77.0325 -12.0955, -77.0340 -12.0955, -77.0340 -12.0940))', 4326),
    1
),
(
    'LT-B-02',
    'MZ-B',
    'P-202',
    'Residencial R4',
    4400.00,
    18.00,
    28.00,
    'Lucía Fernández Silva',
    'Av. El Sol 418',
    'SHP_LT_005',
    geometry::STGeomFromText('POLYGON((-77.0325 -12.0940, -77.0310 -12.0940, -77.0310 -12.0955, -77.0325 -12.0955, -77.0325 -12.0940))', 4326),
    1
),
(
    'LT-B-03',
    'MZ-B',
    'P-203',
    'Equipamiento',
    4400.00,
    22.00,
    30.00,
    'Centro Médico San Isidro',
    'Calle Los Pinos 305',
    'SHP_LT_006',
    geometry::STGeomFromText('POLYGON((-77.0340 -12.0955, -77.0310 -12.0955, -77.0310 -12.0970, -77.0340 -12.0970, -77.0340 -12.0955))', 4326),
    1
),

-- Lotes en Manzana C
(
    'LT-C-01',
    'MZ-C',
    'P-301',
    'Residencial R3',
    3900.00,
    14.00,
    22.00,
    'Roberto Valdivia Paredes',
    'Pasaje Santa Rosa 112',
    'SHP_LT_007',
    geometry::STGeomFromText('POLYGON((-77.0380 -12.0980, -77.0365 -12.0980, -77.0365 -12.0995, -77.0380 -12.0995, -77.0380 -12.0980))', 4326),
    1
),
(
    'LT-C-02',
    'MZ-C',
    'P-302',
    'Residencial R3',
    3900.00,
    14.00,
    22.00,
    'Carmen Rosa Benítez',
    'Pasaje Santa Rosa 124',
    'SHP_LT_008',
    geometry::STGeomFromText('POLYGON((-77.0365 -12.0980, -77.0350 -12.0980, -77.0350 -12.0995, -77.0365 -12.0995, -77.0365 -12.0980))', 4326),
    1
),
(
    'LT-C-03',
    'MZ-C',
    'P-303',
    'Comercial C1',
    4000.75,
    16.00,
    24.00,
    'Farmacias Unidas del Perú',
    'Jr. Las Palmeras 550',
    'SHP_LT_009',
    geometry::STGeomFromText('POLYGON((-77.0380 -12.0995, -77.0350 -12.0995, -77.0350 -12.1010, -77.0380 -12.1010, -77.0380 -12.0995))', 4326),
    1
),

-- Lotes en Manzana D
(
    'LT-D-01',
    'MZ-D',
    'P-401',
    'Residencial R3',
    4300.00,
    16.00,
    26.00,
    'Jorge Luis Huamán Díaz',
    'Av. Los Libertadores 210',
    'SHP_LT_010',
    geometry::STGeomFromText('POLYGON((-77.0340 -12.0980, -77.0325 -12.0980, -77.0325 -12.0995, -77.0340 -12.0995, -77.0340 -12.0980))', 4326),
    1
),
(
    'LT-D-02',
    'MZ-D',
    'P-402',
    'Mixto Residencial-Comercial',
    4300.00,
    16.00,
    26.00,
    'Gastronomía y Sabores S.R.L.',
    'Av. Los Libertadores 228',
    'SHP_LT_011',
    geometry::STGeomFromText('POLYGON((-77.0325 -12.0980, -77.0310 -12.0980, -77.0310 -12.0995, -77.0325 -12.0995, -77.0325 -12.0980))', 4326),
    1
),
(
    'LT-D-03',
    'MZ-D',
    'P-403',
    'Equipamiento',
    4350.20,
    20.00,
    28.00,
    'Parque Infantil Municipal',
    'Jr. Las Palmeras 610',
    'SHP_LT_012',
    geometry::STGeomFromText('POLYGON((-77.0340 -12.0995, -77.0310 -12.0995, -77.0310 -12.1010, -77.0340 -12.1010, -77.0340 -12.0995))', 4326),
    1
);
GO

-- ============================================================================
-- 5. POBLAR CAPA: CODIGOS FIJOS (PUNTOS / HITOS EN WGS 84)
-- ============================================================================
DELETE FROM dbo.CodigosFijos;
GO

INSERT INTO dbo.CodigosFijos (CodigoFijo, TipoPunto, Descripcion, Referencia, CotaZ, OrigenId, Geometria, IdMigracion)
VALUES
(
    'CF-1001',
    'Vértice Geodésico',
    'Punto de Control Geodésico Principal IGN-01',
    'Intersección Av. Los Libertadores con Av. El Sol',
    108.450,
    'SHP_CF_001',
    geometry::STGeomFromText('POINT(-77.0345 -12.0935)', 4326),
    1
),
(
    'CF-1002',
    'Hito Catastral',
    'Hito Límite Sectorial 01-02',
    'Esquina Noreste Manzana B',
    109.120,
    'SHP_CF_002',
    geometry::STGeomFromText('POINT(-77.0308 -12.0938)', 4326),
    1
),
(
    'CF-1003',
    'Poste de Media Tensión',
    'Poste Alumbrado Público N° 5542',
    'Frente a Lote LT-A-01',
    107.890,
    'SHP_CF_003',
    geometry::STGeomFromText('POINT(-77.0375 -12.0945)', 4326),
    1
),
(
    'CF-1004',
    'Cámara de Inspección',
    'Buzón Matriz Alcantarillado SEDAPAL-90',
    'Cruce Calle Los Álamos con Av. Central',
    106.320,
    'SHP_CF_004',
    geometry::STGeomFromText('POINT(-77.0348 -12.0975)', 4326),
    1
),
(
    'CF-1005',
    'Hito Catastral',
    'Vértice Esquina Manzana C',
    'Esquina Suroeste Sector Las Flores',
    105.700,
    'SHP_CF_005',
    geometry::STGeomFromText('POINT(-77.0382 -12.1012)', 4326),
    1
),
(
    'CF-1006',
    'Punto de Nivelación',
    'Placa de Bronce Cota Fija BM-24',
    'Frontis Centro Médico San Isidro',
    108.650,
    'SHP_CF_006',
    geometry::STGeomFromText('POINT(-77.0320 -12.0965)', 4326),
    1
),
(
    'CF-1007',
    'Medidor de Caudal',
    'Válvula Reguladora de Presión VRP-03',
    'Av. Los Libertadores Cdra 2',
    106.150,
    'SHP_CF_007',
    geometry::STGeomFromText('POINT(-77.0335 -12.0988)', 4326),
    1
),
(
    'CF-1008',
    'Punto de Control',
    'Estación de Monitoreo Ambiental EMA-01',
    'Parque Infantil Municipal',
    105.400,
    'SHP_CF_008',
    geometry::STGeomFromText('POINT(-77.0315 -12.1005)', 4326),
    1
);
GO

-- ============================================================================
-- 6. POBLAR CAPA: VIAS (LÍNEAS DE RED VIAL EN WGS 84)
-- ============================================================================
DELETE FROM dbo.Vias;
GO

INSERT INTO dbo.Vias (CodigoVia, Nombre, TipoVia, Jerarquia, Sentido, AnchoViaM, Pavimento, LongitudM, OrigenId, Geometria, IdMigracion)
VALUES
(
    'VIA-01',
    'Los Libertadores',
    'Avenida',
    'Arteria Principal',
    'Doble Sentido',
    24.00,
    'Asfalto en Caliente',
    1150.80,
    'SHP_VIA_001',
    geometry::STGeomFromText('LINESTRING(-77.0390 -12.0935, -77.0345 -12.0935, -77.0300 -12.0935)', 4326),
    1
),
(
    'VIA-02',
    'El Sol',
    'Avenida',
    'Arteria Principal',
    'Doble Sentido',
    22.00,
    'Asfalto en Caliente',
    980.50,
    'SHP_VIA_002',
    geometry::STGeomFromText('LINESTRING(-77.0345 -12.0930, -77.0345 -12.0975, -77.0345 -12.1015)', 4326),
    1
),
(
    'VIA-03',
    'Los Álamos',
    'Calle',
    'Colectora',
    'Un Sentido',
    14.00,
    'Pavimento Rígido',
    520.30,
    'SHP_VIA_003',
    geometry::STGeomFromText('LINESTRING(-77.0390 -12.0975, -77.0345 -12.0975, -77.0300 -12.0975)', 4326),
    1
),
(
    'VIA-04',
    'Los Pinos',
    'Calle',
    'Local',
    'Un Sentido',
    12.00,
    'Adoquín Concreto',
    480.00,
    'SHP_VIA_004',
    geometry::STGeomFromText('LINESTRING(-77.0305 -12.0935, -77.0305 -12.0975, -77.0305 -12.1015)', 4326),
    1
),
(
    'VIA-05',
    'Santa Rosa',
    'Pasaje',
    'Peatonal',
    'Peatonal',
    6.00,
    'Adoquín Decorativo',
    210.40,
    'SHP_VIA_005',
    geometry::STGeomFromText('LINESTRING(-77.0385 -12.0935, -77.0385 -12.0975, -77.0385 -12.1015)', 4326),
    1
),
(
    'VIA-06',
    'Las Palmeras',
    'Jirón',
    'Local',
    'Doble Sentido',
    15.00,
    'Asfalto en Caliente',
    610.75,
    'SHP_VIA_006',
    geometry::STGeomFromText('LINESTRING(-77.0390 -12.1015, -77.0345 -12.1015, -77.0300 -12.1015)', 4326),
    1
);
GO

-- ============================================================================
-- 7. REGISTRO DE EVENTOS INICIALES EN BITÁCORA (RF-SEG-06)
-- ============================================================================
INSERT INTO dbo.BitacoraEventos (FechaHora, Usuario, IpCliente, TipoEvento, Modulo, Descripcion, DetalleTecnico, Exitoso)
VALUES
(
    DATEADD(MINUTE, -29, SYSUTCDATETIME()),
    'admin',
    '127.0.0.1',
    'LoginExitoso',
    'Seguridad',
    'Inicio de sesión exitoso del administrador del sistema.',
    'Autenticación por hash PBKDF2 completada.',
    1
),
(
    DATEADD(MINUTE, -28, SYSUTCDATETIME()),
    'admin',
    '127.0.0.1',
    'Migracion',
    'Migrador',
    'Migración completada exitosamente para Manzanas, Lotes, Códigos Fijos y Vías (30 registros).',
    'Transacción confirmada, validación STIsValid() y SRID 4326 al 100%.',
    1
);
GO

PRINT '>> Catálogo de capas, registro de migración semilla, geometrías de prueba (4 capas) y bitácora cargados exitosamente.';
GO

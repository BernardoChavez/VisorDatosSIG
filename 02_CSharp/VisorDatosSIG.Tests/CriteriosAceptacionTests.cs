#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisorDatosSIG.Application.DTOs.Busqueda;
using VisorDatosSIG.Application.DTOs.Migracion;
using VisorDatosSIG.Application.DTOs.Seguridad;
using VisorDatosSIG.Application.Interfaces.Services;
using VisorDatosSIG.Application.Services;
using VisorDatosSIG.Domain.Entities;
using VisorDatosSIG.Domain.Enums;
using VisorDatosSIG.Domain.ValueObjects;
using VisorDatosSIG.Infrastructure.Data;
using VisorDatosSIG.Infrastructure.Repositories;
using VisorDatosSIG.Infrastructure.Shapefiles;

namespace VisorDatosSIG.Tests;

public static class CriteriosAceptacionTests
{
    private static readonly string TestDatasetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Datasets_Tests_Suite");

    public static void SetupDatasets()
    {
        if (!Directory.Exists(TestDatasetDir))
        {
            Directory.CreateDirectory(TestDatasetDir);
        }
        ShapefileGenerator.GenerarDatasetsDePrueba(TestDatasetDir);
    }

    /// <summary>
    /// CA-01: Validación de componentes requeridos de Shapefiles (.shp, .shx, .dbf, .prj).
    /// </summary>
    public static void Test_CA01_ValidacionComponentesShapefile()
    {
        var reader = new ShapefileReader();
        var shpPath = Path.Combine(TestDatasetDir, "Manzanas.shp");

        var validacion = reader.InspeccionarShapefile(shpPath);

        Assert(validacion.TieneShp, "CA-01: Debe detectar archivo .shp");
        Assert(validacion.TieneShx, "CA-01: Debe detectar archivo .shx");
        Assert(validacion.TieneDbf, "CA-01: Debe detectar archivo .dbf");
        Assert(validacion.TienePrj, "CA-01: Debe detectar archivo .prj");
        Assert(validacion.CantidadRegistros > 0, "CA-01: Debe leer cantidad de registros del .dbf");
        Assert(validacion.CapaEnum == TipoCapa.Manzanas, "CA-01: Debe reconocer la capa Manzanas");

        // Probar shapefile incompleto (simulado)
        var pathInexistente = Path.Combine(TestDatasetDir, "Incompleto.shp");
        var valIncompleto = reader.InspeccionarShapefile(pathInexistente);
        Assert(!valIncompleto.TieneShp, "CA-01: Debe reportar falta de .shp para archivos inexistentes");
        Assert(valIncompleto.Errores.Count > 0, "CA-01: Debe listar errores para datasets incompletos");
    }

    /// <summary>
    /// CA-02: Validación de proyección espacial WGS 84 (EPSG:4326).
    /// </summary>
    public static void Test_CA02_ValidacionProyeccionWgs84()
    {
        var reader = new ShapefileReader();
        var shpPath = Path.Combine(TestDatasetDir, "Lotes.shp");

        var validacion = reader.InspeccionarShapefile(shpPath);

        Assert(validacion.TienePrj, "CA-02: Debe contener archivo .prj");
        Assert(validacion.EsWgs84, "CA-02: Debe validar correctamente la proyección WGS 84");
        Assert(validacion.EsValidoParaCarga, "CA-02: Debe marcar como válido para carga");
    }

    /// <summary>
    /// CA-03: Lectura binaria nativa y generación de WKT para Point, LineString y Polygon.
    /// </summary>
    public static void Test_CA03_LecturaBinariaShapefiles()
    {
        var reader = new ShapefileReader();

        // 1. Puntos (Códigos Fijos)
        var codigos = reader.LeerRegistrosCompletos(Path.Combine(TestDatasetDir, "CodigosFijos.shp")).ToList();
        Assert(codigos.Count == 8, "CA-03: Debe leer 8 puntos de Códigos Fijos");
        Assert(codigos[0].Wkt.StartsWith("POINT("), "CA-03: La geometría debe ser WKT POINT");
        Assert(codigos[0].Atributos.ContainsKey("COD_FIJO"), "CA-03: Debe contener atributo COD_FIJO");

        // 2. Líneas (Vías)
        var vias = reader.LeerRegistrosCompletos(Path.Combine(TestDatasetDir, "Vias.shp")).ToList();
        Assert(vias.Count == 6, "CA-03: Debe leer 6 líneas de Vías");
        Assert(vias[0].Wkt.StartsWith("LINESTRING("), "CA-03: La geometría debe ser WKT LINESTRING");
        Assert(vias[0].Atributos.ContainsKey("NOM_VIA"), "CA-03: Debe contener atributo NOM_VIA");

        // 3. Polígonos (Manzanas y Lotes)
        var lotes = reader.LeerRegistrosCompletos(Path.Combine(TestDatasetDir, "Lotes.shp")).ToList();
        Assert(lotes.Count == 6, "CA-03: Debe leer 6 polígonos de Lotes");
        Assert(lotes[0].Wkt.StartsWith("POLYGON("), "CA-03: La geometría debe ser WKT POLYGON");
    }

    /// <summary>
    /// CA-04: Value Objects espaciales BoundingBox y Coordenada.
    /// </summary>
    public static void Test_CA04_BoundingBoxYCoordenada()
    {
        var coord = new Coordenada(-77.035, -12.095);
        Assert(coord.Longitud == -77.035, "CA-04: Longitud debe coincidir");
        Assert(coord.Latitud == -12.095, "CA-04: Latitud debe coincidir");
        Assert(coord.ToWktPoint().Contains("POINT(-77.035 -12.095)"), "CA-04: Debe generar WKT Point correcto");

        var bbox = new BoundingBox(-77.040, -12.100, -77.030, -12.090);
        Assert(bbox.Contiene(-77.035, -12.095), "CA-04: Debe contener punto interno");
        Assert(!bbox.Contiene(-77.050, -12.095), "CA-04: No debe contener punto externo");
        Assert(bbox.ToWktPolygon().StartsWith("POLYGON("), "CA-04: Debe generar WKT Polygon");

        var parsed = BoundingBox.DesdeCadena("-77.040,-12.100,-77.030,-12.090");
        Assert(parsed.MinLon == -77.040 && parsed.MaxLat == -12.090, "CA-04: Debe parsear correctamente desde cadena");
    }

    /// <summary>
    /// CA-05: Convertidor WKT a GeoJSON estándar RFC 7946.
    /// </summary>
    public static void Test_CA05_ConvertidorWktAGeoJson()
    {
        // Point
        var pointGeo = GeoJsonConverter.WktToGeoJsonGeometry("POINT(-77.0345 -12.0935)");
        Assert(pointGeo != null && pointGeo.Type == "Point", "CA-05: WKT Point a GeoJSON Point");

        // LineString
        var lineGeo = GeoJsonConverter.WktToGeoJsonGeometry("LINESTRING(-77.039 -12.0935, -77.0345 -12.0935, -77.030 -12.0935)");
        Assert(lineGeo != null && lineGeo.Type == "LineString", "CA-05: WKT LineString a GeoJSON LineString");

        // Polygon
        var polyGeo = GeoJsonConverter.WktToGeoJsonGeometry("POLYGON((-77.038 -12.094, -77.035 -12.094, -77.035 -12.097, -77.038 -12.097, -77.038 -12.094))");
        Assert(polyGeo != null && polyGeo.Type == "Polygon", "CA-05: WKT Polygon a GeoJSON Polygon");
    }

    /// <summary>
    /// CA-06: Ingesta transaccional en SQL Server 2022 con SRID 4326 y verificación STIsValid().
    /// </summary>
    public static async Task Test_CA06_IngestaTransaccionalSqlServerAsync(IDbConnectionFactory dbFactory, IMigradorService migrador)
    {
        var req = new IniciarMigracionRequestDto
        {
            RutaCarpetaOShapefile = Path.Combine(TestDatasetDir, "Manzanas.shp"),
            Capa = TipoCapa.Manzanas,
            Modalidad = ModalidadMigracion.Reemplazar,
            Usuario = "test_runner"
        };

        var res = await migrador.EjecutarMigracionAsync(req);
        Assert(res.Estado == "Exitoso", "CA-06: La migración debe completarse con estado Exitoso");
        Assert(res.RegistrosExitosos > 0, "CA-06: Debe registrar inserciones exitosas");
        Assert(res.RegistrosFallidos == 0, "CA-06: No debe haber registros fallidos");
    }

    /// <summary>
    /// CA-07: Modalidades de Migración (Reemplazar vs Anexar evitando duplicados).
    /// </summary>
    public static async Task Test_CA07_ModalidadesMigracionAsync(IDbConnectionFactory dbFactory, IMigradorService migrador)
    {
        // 1. Carga inicial Reemplazar
        var req1 = new IniciarMigracionRequestDto
        {
            RutaCarpetaOShapefile = Path.Combine(TestDatasetDir, "Lotes.shp"),
            Capa = TipoCapa.Lotes,
            Modalidad = ModalidadMigracion.Reemplazar,
            Usuario = "test_runner"
        };
        var res1 = await migrador.EjecutarMigracionAsync(req1);
        Assert(res1.RegistrosExitosos == 6, "CA-07: Carga inicial debe insertar 6 lotes");

        // 2. Carga Anexar con los mismos registros (deben omitirse como duplicados)
        var req2 = new IniciarMigracionRequestDto
        {
            RutaCarpetaOShapefile = Path.Combine(TestDatasetDir, "Lotes.shp"),
            Capa = TipoCapa.Lotes,
            Modalidad = ModalidadMigracion.Anexar,
            Usuario = "test_runner"
        };
        var res2 = await migrador.EjecutarMigracionAsync(req2);
        Assert(res2.RegistrosOmitidos == 6, "CA-07: En modalidad Anexar los registros existentes deben omitirse");
    }

    /// <summary>
    /// CA-08: Búsqueda alfanumérica y espacial con BBox envolvente combinado.
    /// </summary>
    public static async Task Test_CA08_BusquedaEspacialYBBoxCombinadoAsync(IBusquedaService busquedaService)
    {
        var req = new BusquedaRequestDto
        {
            Termino = "Libertadores",
            Pagina = 1,
            RegistrosPorPagina = 20
        };

        var res = await busquedaService.BuscarAsync(req);
        Assert(res.TotalRegistros > 0, "CA-08: La búsqueda de 'Libertadores' debe encontrar coincidencias");
        Assert(res.BboxCombinado != null && res.BboxCombinado.Length == 4, "CA-08: Debe calcular el Bounding Box envolvente combinado");
        Assert(res.BboxCombinado![0] <= res.BboxCombinado[2], "CA-08: BBox MinLon debe ser menor o igual a MaxLon");
    }

    /// <summary>
    /// CA-09: Identificación espacial por punto de clic (Point-in-polygon e intersección).
    /// </summary>
    public static async Task Test_CA09_IdentificacionPorPuntoAsync(IBusquedaService busquedaService)
    {
        var req = new IdentificacionPuntoRequestDto
        {
            Longitud = -77.0370,
            Latitud = -12.0945,
            ToleranciaMetros = 10.0
        };

        var items = await busquedaService.IdentificarPorPuntoAsync(req);
        Assert(items.Count > 0, "CA-09: Debe identificar al menos una entidad (Lote o Manzana) bajo el punto");
        Assert(!string.IsNullOrEmpty(items[0].Codigo), "CA-09: El elemento identificado debe contener su código");
    }

    /// <summary>
    /// CA-10: Criptografía segura PBKDF2-HMAC-SHA512 con 100k iteraciones y Salt.
    /// </summary>
    public static void Test_CA10_CriptografiaPbkdf2()
    {
        var passwordOriginal = "ClaveSecreta2026*!";
        var salt = AuthService.GenerarSalt();
        Assert(!string.IsNullOrEmpty(salt) && salt.Length >= 16, "CA-10: El Salt debe generarse aleatoriamente");

        var hash1 = AuthService.GenerarHash(passwordOriginal, salt);
        var hash2 = AuthService.GenerarHash(passwordOriginal, salt);
        Assert(hash1 == hash2, "CA-10: Hash debe ser determinístico para el mismo Salt y contraseña");

        var esValido = AuthService.VerificarPassword(passwordOriginal, hash1, salt);
        Assert(esValido, "CA-10: Verificación debe ser exitosa con la contraseña correcta");

        var esInvalido = AuthService.VerificarPassword("ClaveErronea123", hash1, salt);
        Assert(!esInvalido, "CA-10: Verificación debe fallar ante contraseñas incorrectas");
    }

    /// <summary>
    /// CA-11: Autenticación, control de accesos y bloqueo de cuenta.
    /// </summary>
    public static async Task Test_CA11_AutenticacionYBloqueoAsync(IAuthService authService)
    {
        // 1. Login exitoso con usuario semilla
        var loginOk = await authService.LoginAsync(new LoginRequestDto { Username = "admin", Password = "Admin123*!" });
        Assert(loginOk.Exitoso, "CA-11: Login debe ser exitoso con credenciales de admin");
        Assert(!string.IsNullOrEmpty(loginOk.Token), "CA-11: Debe generar token de sesión");

        // 2. Login fallido
        var loginFail = await authService.LoginAsync(new LoginRequestDto { Username = "admin", Password = "ClaveTotalmenteErronea" });
        Assert(!loginFail.Exitoso, "CA-11: Login debe ser rechazado con clave incorrecta");
    }

    /// <summary>
    /// CA-12: Trazabilidad y registro inmutable en Bitácora de Auditoría.
    /// </summary>
    public static async Task Test_CA12_BitacoraAuditoriaAsync(IAuditoriaService auditoriaService)
    {
        var eventoDesc = $"Prueba de auditoría automatizada CA-12 ({Guid.NewGuid():N})";
        await auditoriaService.RegistrarEventoAsync("PruebaSuite", "Tests", eventoDesc, "runner_test");

        var bitacora = await auditoriaService.ObtenerBitacoraAsync(new Application.DTOs.Auditoria.BitacoraFiltroDto
        {
            Usuario = "runner_test",
            Pagina = 1,
            RegistrosPorPagina = 10
        });

        Assert(bitacora.TotalRegistros > 0, "CA-12: Debe encontrar eventos registrados en la bitácora");
    }

    /// <summary>
    /// CA-13: Creación de usuarios con permisos granulares dinámicos (PBAC / Feature Flags).
    /// </summary>
    public static async Task Test_CA13_PermisosGranularesYPersonalizacionAsync(IAuthService authService)
    {
        // 1. Obtener catálogo maestro de permisos agrupado
        var catalogo = await authService.ObtenerCatalogoPermisosAgrupadoAsync();
        Assert(catalogo.Count >= 4, "CA-13: El catálogo debe contener al menos 4 categorías (Capas, Herramientas, Funcionalidades, Módulos)");
        Assert(catalogo.Any(c => c.Categoria == "Capas"), "CA-13: Debe existir categoría Capas");
        Assert(catalogo.Any(c => c.Categoria == "Herramientas"), "CA-13: Debe existir categoría Herramientas");

        // 2. Crear usuario personalizado con solo Capa Lotes y GPS
        var testUsername = $"test_campo_{Guid.NewGuid():N}".Substring(0, 18);
        var permisosDeseados = new List<string> { "CAPA_Lotes", "HERR_GPS", "HERR_MEDICION_DISTANCIA" };

        var nuevoUser = await authService.CrearUsuarioAsync(new CrearUsuarioDto
        {
            Username = testUsername,
            Password = "PasswordSeguro123*!",
            NombreCompleto = "Técnico de Campo Test",
            Email = $"{testUsername}@sig.pe",
            IdRol = 2, // Consultor
            Permisos = permisosDeseados
        }, "admin");

        Assert(nuevoUser.IdUsuario > 0, "CA-13: El nuevo usuario debe haber sido creado con ID válido");

        // 3. Iniciar sesión y validar que retorna exactamente los permisos configurados
        var login = await authService.LoginAsync(new LoginRequestDto { Username = testUsername, Password = "PasswordSeguro123*!" });
        Assert(login.Exitoso, "CA-13: Login del usuario personalizado debe ser exitoso");
        Assert(login.Usuario != null, "CA-13: Debe retornar información del usuario");
        Assert(login.Usuario.Permisos.Contains("CAPA_Lotes"), "CA-13: Debe contener permiso CAPA_Lotes");
        Assert(login.Usuario.Permisos.Contains("HERR_GPS"), "CA-13: Debe contener permiso HERR_GPS");
        Assert(!login.Usuario.Permisos.Contains("CAPA_Vias"), "CA-13: NO debe contener permiso de capa no otorgada (CAPA_Vias)");

        // 4. Actualizar permisos granulares (agregar CAPA_Manzanas)
        var permisosActualizados = new List<string> { "CAPA_Lotes", "CAPA_Manzanas", "HERR_GPS" };
        var okActualizar = await authService.ActualizarPermisosUsuarioAsync(nuevoUser.IdUsuario, permisosActualizados, "admin");
        Assert(okActualizar, "CA-13: Debe actualizar los permisos del usuario");

        var permisosLeidos = await authService.ObtenerPermisosUsuarioAsync(nuevoUser.IdUsuario);
        Assert(permisosLeidos.Contains("CAPA_Manzanas"), "CA-13: El usuario debe tener ahora el permiso CAPA_Manzanas");

        // 5. Limpieza / Eliminación del usuario de prueba
        var okEliminar = await authService.EliminarUsuarioAsync(nuevoUser.IdUsuario, "admin");
        Assert(okEliminar, "CA-13: Debe poder eliminar el usuario de prueba correctamente");
    }

    /// <summary>
    /// Restablece de forma idempotente los datos del módulo de agua potable para garantizar aislamiento en pruebas.
    /// </summary>
    public static async Task ResetTestDataAguaAsync(DbConnectionFactory dbFactory)
    {
        using var conn = (System.Data.SqlClient.SqlConnection)await dbFactory.CrearConexionAbiertaAsync();
        const string sql = @"
            DELETE FROM dbo.MonitoreoTrabajador;
            DELETE FROM dbo.AsignacionDetalle;
            DELETE FROM dbo.Asignaciones;
            DELETE FROM dbo.ConexionesAgua;
            DELETE FROM dbo.Trabajadores;

            INSERT INTO dbo.Trabajadores (CodigoTrabajador, NombreCompleto, DocumentoIdentidad, Telefono, TipoTrabajador, Estado, Vehiculo, IdUsuario, Activo)
            VALUES
                ('TRAB-001', 'Juan Pérez Morales',      '8493021 SC', '77012345', 'Cortador',    'Disponible', 'Moto',     NULL, 1),
                ('TRAB-002', 'Roberto Gómez Tapia',    '7102934 SC', '76098765', 'Cortador',    'Disponible', 'Moto',     NULL, 1),
                ('TRAB-003', 'Marcos Suárez Vaca',     '6192834 SC', '71034567', 'Reconector',  'Disponible', 'Moto',     NULL, 1),
                ('TRAB-004', 'David Rojas Peña',       '5918273 SC', '78045612', 'Reconector',  'Disponible', 'A pie',    NULL, 1);

            DECLARE @IdLoteA1 INT = (SELECT TOP 1 IdLote FROM dbo.Lotes WHERE CodigoLote = 'LT-A-01');
            DECLARE @IdLoteA2 INT = (SELECT TOP 1 IdLote FROM dbo.Lotes WHERE CodigoLote = 'LT-A-02');
            DECLARE @IdLoteA3 INT = (SELECT TOP 1 IdLote FROM dbo.Lotes WHERE CodigoLote = 'LT-A-03');
            DECLARE @IdLoteB1 INT = (SELECT TOP 1 IdLote FROM dbo.Lotes WHERE CodigoLote = 'LT-B-01');
            DECLARE @IdLoteB2 INT = (SELECT TOP 1 IdLote FROM dbo.Lotes WHERE CodigoLote = 'LT-B-02');
            DECLARE @IdLoteB3 INT = (SELECT TOP 1 IdLote FROM dbo.Lotes WHERE CodigoLote = 'LT-B-03');

            INSERT INTO dbo.ConexionesAgua 
            (CodigoConexion, NumeroMedidor, IdLote, CodigoLote, CodigoManzana, Titular, Direccion, Categoria, EstadoConexion, MesesMora, SaldoPendiente, UltimaLectura, FechaUltimaLectura, Longitud, Latitud, Geometria, Activo)
            VALUES
                ('CON-1001', 'MED-5541', @IdLoteA1, 'LT-A-01', 'MZ-A', 'Carlos Mendoza Ramos',        'Av. Los Libertadores 142', 'Doméstico',  'ParaCorte',   3, 345.50, 482.30, '2026-09-15', -77.03725, -12.09475, GEOMETRY::Point(-77.03725, -12.09475, 4326), 1),
                ('CON-1002', 'MED-5542', @IdLoteA2, 'LT-A-02', 'MZ-A', 'Inmobiliaria El Roble S.A.C.', 'Av. Los Libertadores 156', 'Comercial',  'Normal',      0,   0.00, 1250.80, '2026-10-01', -77.03600, -12.09475, GEOMETRY::Point(-77.03600, -12.09475, 4326), 1),
                ('CON-1003', 'MED-5543', @IdLoteA3, 'LT-A-03', 'MZ-A', 'Elena Gutiérrez Vega',        'Calle Los Álamos 210',     'Doméstico',  'Cortado',     5, 620.00, 315.00, '2026-08-20', -77.03600, -12.09520, GEOMETRY::Point(-77.03600, -12.09520, 4326), 1),
                ('CON-1004', 'MED-5544', @IdLoteB1, 'LT-B-01', 'MZ-B', 'Banco Metropolitano',          'Av. El Sol 402',           'Comercial',  'ParaCorte',   2, 890.20, 2450.10, '2026-09-28', -77.03750, -12.09350, GEOMETRY::Point(-77.03750, -12.09350, 4326), 1),
                ('CON-1005', 'MED-5545', @IdLoteB2, 'LT-B-02', 'MZ-B', 'Lucía Fernández Silva',       'Av. El Sol 418',           'Doméstico',  'Normal',      0,   0.00, 198.40, '2026-10-05', -77.03650, -12.09350, GEOMETRY::Point(-77.03650, -12.09350, 4326), 1),
                ('CON-1006', 'MED-5546', @IdLoteB3, 'LT-B-03', 'MZ-B', 'Centro Médico San Isidro',     'Calle Los Pinos 305',      'Equipamiento', 'BajaParcial', 0,   0.00, 89.00, '2026-07-10', -77.03550, -12.09350, GEOMETRY::Point(-77.03550, -12.09350, 4326), 1),
                ('CON-1007', 'MED-5547', NULL, 'LT-C-01', 'MZ-C', 'Guillermo Paz Soldán',       'Av. Principal 550',        'Doméstico',  'ParaCorte',   4, 410.00, 620.50, '2026-08-30', -77.03850, -12.09600, GEOMETRY::Point(-77.03850, -12.09600, 4326), 1),
                ('CON-1008', 'MED-5548', NULL, 'LT-C-02', 'MZ-C', 'Farmacia Santa María',       'Av. Principal 562',        'Comercial',  'Cortado',     1, 150.00, 440.00, '2026-10-10', -77.03750, -12.09600, GEOMETRY::Point(-77.03750, -12.09600, 4326), 1),
                ('CON-1009', 'MED-5549', NULL, 'LT-D-01', 'MZ-D', 'Jorge Valenzuela Prado',     'Pasaje Santa Rosa 12',     'Doméstico',  'Normal',      0,   0.00, 312.20, '2026-10-02', -77.03450, -12.09450, GEOMETRY::Point(-77.03450, -12.09450, 4326), 1),
                ('CON-1010', 'MED-5550', NULL, 'LT-D-02', 'MZ-D', 'Panadería La Espiga',        'Pasaje Santa Rosa 24',     'Comercial',  'ParaCorte',   2, 290.00, 890.00, '2026-09-25', -77.03350, -12.09450, GEOMETRY::Point(-77.03350, -12.09450, 4326), 1);

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
        ";
        using var cmd = new System.Data.SqlClient.SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// CA-14: Modelo de datos de agua potable, conexiones georreferenciadas y estados de servicio.
    /// </summary>
    public static async Task Test_CA14_ModuloAguaDominioYConexionesAsync(IAguaService aguaService)
    {
        // 1. Dashboard de Agua
        var dashboard = await aguaService.ObtenerDashboardAsync();
        Assert(dashboard.TotalConexiones >= 10, $"CA-14: Debe haber al menos 10 conexiones en BD (obtenidas: {dashboard.TotalConexiones})");
        Assert(dashboard.ConexionesParaCorte > 0, "CA-14: Debe haber conexiones con estado ParaCorte");
        Assert(dashboard.ConexionesCortadas > 0, "CA-14: Debe haber conexiones con estado Cortado");
        Assert(dashboard.TotalTrabajadores >= 4, "CA-14: Debe haber al menos 4 operarios de campo registrados");

        // 2. Filtro de conexiones por estado
        var paraCorte = await aguaService.ObtenerConexionesAsync("ParaCorte");
        Assert(paraCorte.Count > 0, "CA-14: Debe recuperar conexiones ParaCorte");
        Assert(paraCorte.All(c => c.EstadoConexion == "ParaCorte"), "CA-14: Todas las conexiones filtradas deben tener estado ParaCorte");
        Assert(paraCorte.All(c => c.Longitud != 0 && c.Latitud != 0), "CA-14: Todas las conexiones deben tener coordenadas WGS84");

        // 3. Trabajadores por tipo
        var cortadores = await aguaService.ObtenerTrabajadoresAsync("Cortador");
        var reconectores = await aguaService.ObtenerTrabajadoresAsync("Reconector");
        Assert(cortadores.Count >= 2, "CA-14: Debe haber al menos 2 cortadores");
        Assert(reconectores.Count >= 2, "CA-14: Debe haber al menos 2 reconectores");
    }

    /// <summary>
    /// CA-15: Algoritmo de Dijkstra sobre red vial (dbo.Vias) para generar ruta secuencial (1, 2, 3...) sin cruzar manzanas.
    /// </summary>
    public static async Task Test_CA15_DijkstraRoutingRedVialAsync(IAguaService aguaService, IDijkstraRoutingService dijkstraService)
    {
        // 1. Obtener candidatos para corte desde el almacén central
        double inicioLon = -77.03450;
        double inicioLat = -12.09350;
        var candidatos = await aguaService.ObtenerCandidatosAsignacionAsync("Corte", 3, inicioLon, inicioLat);
        Assert(candidatos.Count >= 2, "CA-15: Debe haber al menos 2 candidatos para generar ruta de prueba");

        // 2. Calcular ruta óptima con Dijkstra
        var ruta = await dijkstraService.CalcularRutaOptimaAsync(inicioLon, inicioLat, candidatos);
        Assert(ruta.DistanciaTotalMetros > 0, "CA-15: La distancia calculada debe ser mayor a 0 metros");
        Assert(!string.IsNullOrWhiteSpace(ruta.RutaLineStringWkt), "CA-15: Debe generar WKT LINESTRING para la red de vías");
        Assert(ruta.RutaLineStringWkt.StartsWith("LINESTRING", StringComparison.OrdinalIgnoreCase), "CA-15: El formato de la geometría debe ser LINESTRING");
        Assert(ruta.PuntosOrdenados.Count == candidatos.Count, "CA-15: Todos los puntos destino deben estar en la ruta ordenada");

        // 3. Verificar orden secuencial estricto 1, 2, 3...
        for (int i = 0; i < ruta.PuntosOrdenados.Count; i++)
        {
            Assert(ruta.PuntosOrdenados[i].OrdenSecuencia == i + 1, $"CA-15: Secuencia debe ser {i + 1} para el punto {i}");
            Assert(ruta.PuntosOrdenados[i].DistanciaTramoMetros >= 0, "CA-15: Distancia de tramo no puede ser negativa");
        }
    }

    /// <summary>
    /// CA-16: Asignación de órdenes de trabajo y validación geodésica estricta de GPS móvil en campo.
    /// </summary>
    public static async Task Test_CA16_AsignacionesYValidacionGpsCampoAsync(IAguaService aguaService)
    {
        // 1. Obtener un cortador disponible
        var trabajadores = await aguaService.ObtenerTrabajadoresAsync("Cortador");
        var cortador = trabajadores.First();

        // 2. Obtener 2 candidatos para corte
        var candidatos = await aguaService.ObtenerCandidatosAsignacionAsync("Corte", 2, -77.03450, -12.09350);
        Assert(candidatos.Count > 0, "CA-16: Debe haber conexiones disponibles para asignar");

        // 3. Crear asignación con ruta óptima
        var request = new Application.DTOs.Agua.CrearAsignacionRequestDto
        {
            IdTrabajador = cortador.IdTrabajador,
            TipoServicio = "Corte",
            PuntoPartidaNombre = "Base Central",
            PuntoPartidaLon = -77.03450,
            PuntoPartidaLat = -12.09350,
            IdConexiones = candidatos.Select(c => c.IdConexion).ToList(),
            Notas = "Ruta de prueba automatizada CA-16",
            Usuario = "admin_test"
        };

        var asignacion = await aguaService.CrearAsignacionConRutaAsync(request);
        Assert(asignacion.IdAsignacion > 0, "CA-16: Debe retornar ID de asignación generado");
        Assert(asignacion.CodigoAsignacion.StartsWith("ASIG-"), "CA-16: Código de asignación debe iniciar con ASIG-");
        Assert(asignacion.Puntos.Count == candidatos.Count, "CA-16: Debe incluir los puntos solicitados");

        // 4. Validar ejecución en campo con GPS PRESENT EN SITIO (<= 60 metros)
        var primerPunto = asignacion.Puntos.First();
        var respExito = await aguaService.RegistrarEjecucionCampoAsync(new Application.DTOs.Agua.RegistroEjecucionCampoRequestDto
        {
            IdDetalle = primerPunto.IdDetalle,
            EstadoResultado = "Ejecutado",
            LecturaActual = 520.40m,
            NumeroMedidorReportado = primerPunto.NumeroMedidor,
            FotoMedidorUrl = "/evidencias/corte_medidor_01.jpg",
            Observacion = "Corte realizado con éxito",
            GpsLat = primerPunto.Latitud + 0.00005, // Aprox ~5 metros del medidor
            GpsLon = primerPunto.Longitud + 0.00005,
            ToleranciaMetrosMax = 60.0
        });

        Assert(respExito.Exito, "CA-16: El registro de ejecución debe ser exitoso");
        Assert(respExito.ValidacionGpsExitosa, "CA-16: La validación GPS debe ser exitosa dentro de la tolerancia de 60m");
        Assert(respExito.DistanciaMetros <= 60.0m, "CA-16: Distancia calculada debe ser menor o igual a 60m");

        // 5. Validar ejecución en campo con reporte de 'CasaCerrada' (también exige GPS)
        if (asignacion.Puntos.Count > 1)
        {
            var segundoPunto = asignacion.Puntos[1];
            var respCasaCerrada = await aguaService.RegistrarEjecucionCampoAsync(new Application.DTOs.Agua.RegistroEjecucionCampoRequestDto
            {
                IdDetalle = segundoPunto.IdDetalle,
                EstadoResultado = "CasaCerrada",
                Observacion = "Predio sin acceso, reja cerrada y perro bravo",
                GpsLat = segundoPunto.Latitud,
                GpsLon = segundoPunto.Longitud,
                ToleranciaMetrosMax = 60.0
            });

            Assert(respCasaCerrada.Exito, "CA-16: El reporte de CasaCerrada debe ser registrado");
            Assert(respCasaCerrada.ValidacionGpsExitosa, "CA-16: El reporte CasaCerrada debe confirmar presencia en el sitio (ValidacionGpsExitosa = true)");
        }
    }

    /// <summary>
    /// CA-17: Monitoreo y telemetría de operarios en tiempo real para panel de control central.
    /// </summary>
    public static async Task Test_CA17_MonitoreoTiempoRealAsync(IAguaService aguaService)
    {
        var trabajadores = await aguaService.ObtenerTrabajadoresAsync();
        var primerTrab = trabajadores.First();

        // 1. Enviar telemetría GPS del operario
        bool actOk = await aguaService.ActualizarUbicacionMonitoreoAsync(new Application.DTOs.Agua.ActualizarUbicacionGpsRequestDto
        {
            IdTrabajador = primerTrab.IdTrabajador,
            Latitud = -12.09450,
            Longitud = -77.03720,
            VelocidadKmH = 24.5,
            Rumbo = 135.0,
            PrecisionM = 3.8,
            BateriaPorcentaje = 84
        });
        Assert(actOk, "CA-17: Debe actualizar la telemetría GPS del operario en tiempo real");

        // 2. Consultar lista de monitoreo administrativo
        var monitoreo = await aguaService.ObtenerMonitoreoOperariosAsync();
        Assert(monitoreo.Count > 0, "CA-17: Debe consultar la lista de monitoreo de operarios");

        var op = monitoreo.FirstOrDefault(m => m.IdTrabajador == primerTrab.IdTrabajador);
        Assert(op != null, "CA-17: El operario debe estar presente en el panel de monitoreo");
        Assert(op!.EnLinea, "CA-17: El operario debe figurar En Línea");
        Assert(op.BateriaPorcentaje == 84, "CA-17: El porcentaje de batería debe coincidir con la última telemetría");
        Assert(Math.Abs(op.Latitud - (-12.09450)) < 0.0001, "CA-17: Latitud debe coincidir");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"[FALLO DE ASERCIÓN] {message}");
        }
    }
}

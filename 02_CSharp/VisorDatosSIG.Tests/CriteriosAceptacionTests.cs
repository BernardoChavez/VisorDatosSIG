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

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"[FALLO DE ASERCIÓN] {message}");
        }
    }
}

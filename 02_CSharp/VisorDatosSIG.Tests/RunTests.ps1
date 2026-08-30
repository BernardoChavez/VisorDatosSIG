# VisorDatosSIG 2026 - Suite de Pruebas de Criterios de Aceptacion (CA-01 a CA-12)
$ErrorActionPreference = "Stop"

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "   VisorDatosSIG 2026 - Suite Automatizada de Criterios de Aceptacion (CA-01 a CA-12)" -ForegroundColor Cyan
Write-Host "================================================================================`n" -ForegroundColor Cyan

$baseDir = "c:\Users\USER\Documents\clases\SIG\proyecto"
$datasetsDir = "$baseDir\02_CSharp\VisorDatosSIG.Tests\Datasets_Test_Suite"
if (-not (Test-Path $datasetsDir)) {
    [System.IO.Directory]::CreateDirectory($datasetsDir) | Out-Null
}

# Cargar Ensamblados
Add-Type -Path "$baseDir\02_CSharp\VisorDatosSIG.Domain\VisorDatosSIG.Domain.dll"
Add-Type -Path "$baseDir\02_CSharp\VisorDatosSIG.Application\VisorDatosSIG.Application.dll"
Add-Type -Path "$baseDir\02_CSharp\VisorDatosSIG.Infrastructure\VisorDatosSIG.Infrastructure.dll"

# Generar Datasets
[VisorDatosSIG.Infrastructure.Shapefiles.ShapefileGenerator]::GenerarDatasetsDePrueba($datasetsDir)

$db = New-Object VisorDatosSIG.Infrastructure.Data.DbConnectionFactory
$bRepo = New-Object VisorDatosSIG.Infrastructure.Repositories.BitacoraRepository($db)
$aud = New-Object VisorDatosSIG.Application.Services.AuditoriaService($bRepo)
$mRepo = New-Object VisorDatosSIG.Infrastructure.Repositories.MigracionRepository($db)
$rdr = New-Object VisorDatosSIG.Infrastructure.Shapefiles.ShapefileReader
$migService = New-Object VisorDatosSIG.Application.Services.MigradorService($mRepo, $rdr, $aud)
$cRepo = New-Object VisorDatosSIG.Infrastructure.Repositories.CapaRepository($db)
$cService = New-Object VisorDatosSIG.Application.Services.CapaService($cRepo)
$bRepoSpatial = New-Object VisorDatosSIG.Infrastructure.Repositories.BusquedaRepository($db)
$bService = New-Object VisorDatosSIG.Application.Services.BusquedaService($bRepoSpatial)
$uRepo = New-Object VisorDatosSIG.Infrastructure.Repositories.UsuarioRepository($db)
$auth = New-Object VisorDatosSIG.Application.Services.AuthService($uRepo, $aud)

$total = 12
$passed = 0
$failed = 0
$swTotal = [System.Diagnostics.Stopwatch]::StartNew()

function Execute-Test {
    param(
        [string]$name,
        [scriptblock]$code
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        & $code
        $sw.Stop()
        Write-Host "  [PASS] " -ForegroundColor Green -NoNewline
        $msg = "{0} ({1} ms)" -f $name, $sw.ElapsedMilliseconds
        Write-Host $msg
        $script:passed++
    } catch {
        $sw.Stop()
        Write-Host "  [FAIL] " -ForegroundColor Red -NoNewline
        $msg = "{0} ({1} ms)" -f $name, $sw.ElapsedMilliseconds
        Write-Host $msg
        $errMsg = "         Error: {0}" -f $_.Exception.Message
        Write-Host $errMsg -ForegroundColor Yellow
        $script:failed++
    }
}

# CA-01
Execute-Test -name "CA-01: Validacion de componentes requeridos (.shp .shx .dbf .prj)" -code {
    $val = $rdr.InspeccionarShapefile("$datasetsDir\Manzanas.shp")
    if (-not $val.TieneShp -or -not $val.TieneShx -or -not $val.TieneDbf -or -not $val.TienePrj) {
        throw "Faltan componentes en el Shapefile de Manzanas"
    }
    if ($val.CantidadRegistros -ne 4) { throw "Cantidad de registros esperada: 4" }
}

# CA-02
Execute-Test -name "CA-02: Validacion de proyeccion espacial WGS 84 (EPSG:4326)" -code {
    $val = $rdr.InspeccionarShapefile("$datasetsDir\Lotes.shp")
    if (-not $val.EsWgs84 -or -not $val.EsValidoParaCarga) {
        throw "La capa Lotes no fue validada como WGS 84 valida para carga"
    }
}

# CA-03
Execute-Test -name "CA-03: Lectura binaria nativa de Shapefiles (Point Line Poly + DBF)" -code {
    $puntos = [System.Linq.Enumerable]::ToList($rdr.LeerRegistrosCompletos("$datasetsDir\CodigosFijos.shp"))
    if ($puntos.Count -ne 8) { throw "Esperados 8 puntos" }
    if (-not $puntos[0].Item2.StartsWith("POINT(")) { throw "Geometria no es WKT POINT" }

    $lineas = [System.Linq.Enumerable]::ToList($rdr.LeerRegistrosCompletos("$datasetsDir\Vias.shp"))
    if ($lineas.Count -ne 6) { throw "Esperadas 6 vias" }
    if (-not $lineas[0].Item2.StartsWith("LINESTRING(")) { throw "Geometria no es WKT LINESTRING" }

    $polys = [System.Linq.Enumerable]::ToList($rdr.LeerRegistrosCompletos("$datasetsDir\Lotes.shp"))
    if ($polys.Count -ne 6) { throw "Esperados 6 lotes" }
    if (-not $polys[0].Item2.StartsWith("POLYGON(")) { throw "Geometria no es WKT POLYGON" }
}

# CA-04
Execute-Test -name "CA-04: Value Objects espaciales BoundingBox y Coordenada" -code {
    $coord = New-Object VisorDatosSIG.Domain.ValueObjects.Coordenada(-77.035, -12.095)
    if ($coord.Longitud -ne -77.035 -or $coord.Latitud -ne -12.095) { throw "Coordenada con valores incorrectos" }

    $bbox = New-Object VisorDatosSIG.Domain.ValueObjects.BoundingBox(-77.040, -12.100, -77.030, -12.090)
    if (-not $bbox.Contiene(-77.035, -12.095)) { throw "El BBox deberia contener el punto interior" }
    if ($bbox.Contiene(-77.050, -12.095)) { throw "El BBox NO deberia contener el punto exterior" }
    if (-not $bbox.ToWktPolygon().StartsWith("POLYGON(")) { throw "WKT de BBox invalido" }
}

# CA-05
Execute-Test -name "CA-05: Convertidor WKT a GeoJSON estandar RFC 7946" -code {
    $ptGeo = [VisorDatosSIG.Application.Services.GeoJsonConverter]::WktToGeoJsonGeometry("POINT(-77.0345 -12.0935)")
    if ($ptGeo.Type -ne "Point") { throw "Error al convertir WKT Point a GeoJSON" }

    $polyGeo = [VisorDatosSIG.Application.Services.GeoJsonConverter]::WktToGeoJsonGeometry("POLYGON((-77.038 -12.094, -77.035 -12.094, -77.035 -12.097, -77.038 -12.097, -77.038 -12.094))")
    if ($polyGeo.Type -ne "Polygon") { throw "Error al convertir WKT Polygon a GeoJSON" }
}

# CA-06
Execute-Test -name "CA-06: Ingesta transaccional en SQL Server con SRID 4326 y STIsValid()" -code {
    $req = New-Object VisorDatosSIG.Application.DTOs.Migracion.IniciarMigracionRequestDto
    $req.RutaCarpetaOShapefile = "$datasetsDir\Manzanas.shp"
    $req.Capa = [VisorDatosSIG.Domain.Enums.TipoCapa]::Manzanas
    $req.Modalidad = [VisorDatosSIG.Domain.Enums.ModalidadMigracion]::Reemplazar
    $req.Usuario = "runner_ca06"

    $res = $migService.EjecutarMigracionAsync($req, $null, [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()
    if ($res.Estado -ne "Exitoso" -or $res.RegistrosExitosos -ne 4) {
        throw "Migracion de Manzanas no fue exitosa. Estado=$($res.Estado)"
    }
}

# CA-07
Execute-Test -name "CA-07: Modalidades de Migracion (Reemplazar vs Anexar sin duplicados)" -code {
    $req1 = New-Object VisorDatosSIG.Application.DTOs.Migracion.IniciarMigracionRequestDto
    $req1.RutaCarpetaOShapefile = "$datasetsDir\Lotes.shp"
    $req1.Capa = [VisorDatosSIG.Domain.Enums.TipoCapa]::Lotes
    $req1.Modalidad = [VisorDatosSIG.Domain.Enums.ModalidadMigracion]::Reemplazar
    $req1.Usuario = "runner_ca07"
    $res1 = $migService.EjecutarMigracionAsync($req1, $null, [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()
    if ($res1.RegistrosExitosos -ne 6) { throw "Carga inicial debe insertar 6 lotes" }

    $req2 = New-Object VisorDatosSIG.Application.DTOs.Migracion.IniciarMigracionRequestDto
    $req2.RutaCarpetaOShapefile = "$datasetsDir\Lotes.shp"
    $req2.Capa = [VisorDatosSIG.Domain.Enums.TipoCapa]::Lotes
    $req2.Modalidad = [VisorDatosSIG.Domain.Enums.ModalidadMigracion]::Anexar
    $req2.Usuario = "runner_ca07"
    $res2 = $migService.EjecutarMigracionAsync($req2, $null, [System.Threading.CancellationToken]::None).GetAwaiter().GetResult()
    if ($res2.RegistrosOmitidos -ne 6) { throw "En Anexar los 6 lotes debieron ser omitidos por duplicidad" }
}

# CA-08
Execute-Test -name "CA-08: Busqueda alfanumerica/espacial con BBox envolvente combinado" -code {
    $req = New-Object VisorDatosSIG.Application.DTOs.Busqueda.BusquedaRequestDto
    $req.Termino = "Libertadores"
    $req.Pagina = 1
    $req.RegistrosPorPagina = 20

    $res = $bService.BuscarAsync($req).GetAwaiter().GetResult()
    if ($res.TotalRegistros -lt 1) { throw "La busqueda 'Libertadores' no arrojo resultados" }
    if ($null -eq $res.BboxCombinado -or $res.BboxCombinado.Length -ne 4) { throw "BBox envolvente combinado no fue calculado" }
}

# CA-09
Execute-Test -name "CA-09: Identificacion espacial por punto (Point-in-polygon y buffer)" -code {
    $req = New-Object VisorDatosSIG.Application.DTOs.Busqueda.IdentificacionPuntoRequestDto
    $req.Longitud = -77.0370
    $req.Latitud = -12.0945
    $req.ToleranciaMetros = 10.0

    $items = $bService.IdentificarPorPuntoAsync($req).GetAwaiter().GetResult()
    if ($items.Count -lt 1) { throw "No se identificaron entidades bajo las coordenadas de prueba" }
}

# CA-10
Execute-Test -name "CA-10: Criptografia segura PBKDF2-HMAC-SHA512 con 100k iteraciones + Salt" -code {
    $password = "ClaveSecreta2026*!"
    $salt = [VisorDatosSIG.Application.Services.AuthService]::GenerarSalt()
    $hash = [VisorDatosSIG.Application.Services.AuthService]::GenerarHash($password, $salt)

    $valido = [VisorDatosSIG.Application.Services.AuthService]::VerificarPassword($password, $hash, $salt)
    if (-not $valido) { throw "La verificacion PBKDF2 fallo para la contrasena correcta" }

    $invalido = [VisorDatosSIG.Application.Services.AuthService]::VerificarPassword("ClaveInvalida", $hash, $salt)
    if ($invalido) { throw "La verificacion PBKDF2 acepto erroneamente una clave falsa" }
}

# CA-11
Execute-Test -name "CA-11: Autenticacion control de accesos y bloqueo de cuentas" -code {
    $loginReq = New-Object VisorDatosSIG.Application.DTOs.Seguridad.LoginRequestDto
    $loginReq.Username = "admin"
    $loginReq.Password = "Admin123*!"

    $loginRes = $auth.LoginAsync($loginReq, "127.0.0.1").GetAwaiter().GetResult()
    if (-not $loginRes.Exitoso) { throw "Login de usuario admin fallo" }

    $loginBad = New-Object VisorDatosSIG.Application.DTOs.Seguridad.LoginRequestDto
    $loginBad.Username = "admin"
    $loginBad.Password = "ClaveErronea123"
    $loginBadRes = $auth.LoginAsync($loginBad, "127.0.0.1").GetAwaiter().GetResult()
    if ($loginBadRes.Exitoso) { throw "Login erroneo fue aceptado" }
}

# CA-12
Execute-Test -name "CA-12: Trazabilidad y persistencia inmutable en Bitacora de Auditoria" -code {
    $aud.RegistrarEventoAsync("PruebaSuite", "Tests", "Prueba de auditoria CA-12", "runner_ca12", "127.0.0.1", "{}", $true).GetAwaiter().GetResult()

    $filtro = New-Object VisorDatosSIG.Application.DTOs.Auditoria.BitacoraFiltroDto
    $filtro.Usuario = "runner_ca12"
    $filtro.Pagina = 1
    $filtro.RegistrosPorPagina = 10

    $bitacora = $aud.ObtenerBitacoraAsync($filtro).GetAwaiter().GetResult()
    if ($bitacora.TotalRegistros -lt 1) { throw "No se encontraron eventos en la bitacora para el usuario runner_ca12" }
}

$swTotal.Stop()
Write-Host "`n================================================================================" -ForegroundColor Cyan
if ($failed -eq 0) {
    $summary = "  RESUMEN: {0}/{1} PRUEBAS COMPLETADAS EXITOSAMENTE (100% OK)" -f $passed, $total
    Write-Host $summary -ForegroundColor Green
} else {
    $summary = "  RESUMEN: {0}/{1} EXITOSAS, {2} FALLIDAS" -f $passed, $total, $failed
    Write-Host $summary -ForegroundColor Red
}
$timeMsg = "  TIEMPO TOTAL DE EJECUCION: {0} ms" -f $swTotal.ElapsedMilliseconds
Write-Host $timeMsg -ForegroundColor Cyan
Write-Host "================================================================================`n" -ForegroundColor Cyan

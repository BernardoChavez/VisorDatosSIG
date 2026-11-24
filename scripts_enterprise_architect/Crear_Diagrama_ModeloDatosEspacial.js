!INC Local Scripts.EAConstants-JScript

/**
 * ==============================================================================
 * SISTEMA: VisorDatosSIG 2026 - UAGRM FICCT
 * SCRIPT: Generador del Modelo de Datos Espacial (Base de Datos ERD)
 * ==============================================================================
 */

function main() {
    Repository.EnsureOutputVisible("Script");
    Session.Output(">> Generando Modelo de Datos Espacial (ERD SQL Server Spatial)...");

    var targetPackage as EA.Package;
    targetPackage = Repository.GetTreeSelectedPackage();

    if (targetPackage == null) {
        Session.Prompt("Seleccione un paquete en el Project Browser.", promptOK);
        return;
    }

    var pkgERD as EA.Package;
    pkgERD = targetPackage.Packages.AddNew("2.4.2. Modelo de Datos Espacial", "");
    pkgERD.Update();

    var diag as EA.Diagram;
    diag = pkgERD.Diagrams.AddNew("Modelo Entidad-Relación Espacial (SQL Server 2022 Spatial)", "Logical");
    diag.Update();

    // 1. Manzanas
    var tManzanas = crearTabla(pkgERD, "Manzanas", [
        "PK IdManzana : int",
        "CodigoManzana : varchar(50) [UQ]",
        "Sector : varchar(100)",
        "Barrio : varchar(100)",
        "PerimetroM : decimal(12,2)",
        "SuperficieM2 : decimal(12,2)",
        "SPATIAL Geometria : geometry (Polygon SRID 4326)"
    ]);
    addObj(diag, tManzanas, 50, 50, 320, 230);

    // 2. Lotes
    var tLotes = crearTabla(pkgERD, "Lotes", [
        "PK IdLote : int",
        "CodigoLote : varchar(50) [UQ]",
        "FK IdManzana : int",
        "NumeroPredio : varchar(50)",
        "Titular : varchar(150)",
        "Direccion : varchar(200)",
        "UsoSuelo : varchar(50)",
        "FrenteM : decimal(10,2)",
        "FondoM : decimal(10,2)",
        "SuperficieM2 : decimal(12,2)",
        "SPATIAL Geometria : geometry (Polygon SRID 4326)"
    ]);
    addObj(diag, tLotes, 400, 50, 700, 290);

    // 3. ConexionesAgua
    var tConexiones = crearTabla(pkgERD, "ConexionesAgua", [
        "PK IdConexion : int",
        "CodigoConexion : varchar(50) [UQ]",
        "NumeroMedidor : varchar(50) [UQ]",
        "FK IdLote : int",
        "Titular : varchar(150)",
        "Direccion : varchar(200)",
        "Categoria : varchar(50)",
        "EstadoConexion : varchar(30) (Normal/ParaCorte/Cortado)",
        "MesesMora : int",
        "SaldoPendiente : decimal(12,2)",
        "UltimaLectura : decimal(10,2)",
        "Longitud : float",
        "Latitud : float",
        "SPATIAL Geometria : geometry (Point SRID 4326)"
    ]);
    addObj(diag, tConexiones, 780, 50, 1100, 360);

    // 4. Vias
    var tVias = crearTabla(pkgERD, "Vias", [
        "PK IdVia : int",
        "NombreVia : varchar(150)",
        "TipoVia : varchar(50) (Avenida, Calle, Jiron)",
        "Jerarquia : varchar(50) (Arteria, Local)",
        "Sentido : varchar(30)",
        "Pavimento : varchar(50) (Asfalto, Concreto)",
        "AnchoMetros : decimal(8,2)",
        "SPATIAL Geometria : geometry (LineString SRID 4326)"
    ]);
    addObj(diag, tVias, 50, 320, 320, 500);

    // 5. CodigosFijos
    var tCodFijos = crearTabla(pkgERD, "CodigosFijos", [
        "PK IdCodigoFijo : int",
        "Codigo : varchar(50) [UQ]",
        "Tipo : varchar(50) (Hito, Vertice, Camara)",
        "CotaZ : decimal(10,3)",
        "Referencia : varchar(200)",
        "SPATIAL Geometria : geometry (Point SRID 4326)"
    ]);
    addObj(diag, tCodFijos, 50, 560, 320, 710);

    // 6. Trabajadores
    var tTrabajadores = crearTabla(pkgERD, "Trabajadores", [
        "PK IdTrabajador : int",
        "CodigoTrabajador : varchar(50) [UQ]",
        "NombreCompleto : varchar(150)",
        "Telefono : varchar(30)",
        "TipoTrabajador : varchar(30) (Cortador/Reconector)",
        "Estado : varchar(30) (Disponible/EnRuta)",
        "Vehiculo : varchar(50) (Moto/A pie)",
        "FK IdUsuario : int"
    ]);
    addObj(diag, tTrabajadores, 780, 420, 1100, 600);

    // 7. Asignaciones
    var tAsignaciones = crearTabla(pkgERD, "Asignaciones", [
        "PK IdAsignacion : int",
        "CodigoAsignacion : varchar(50) [UQ]",
        "FK IdTrabajador : int",
        "TipoServicio : varchar(30) (Corte/Reconexion)",
        "FechaAsignacion : date",
        "Estado : varchar(30) (Pendiente/EnProgreso/Completada)",
        "CantidadPuntos : int",
        "DistanciaTotalMetros : decimal(12,2)",
        "RutaOptimaWkt : varchar(max) (Calculada Dijkstra)"
    ]);
    addObj(diag, tAsignaciones, 400, 360, 700, 580);

    // 8. AsignacionDetalle
    var tAsigDetalle = crearTabla(pkgERD, "AsignacionDetalle", [
        "PK IdDetalle : int",
        "FK IdAsignacion : int",
        "FK IdConexion : int",
        "OrdenSecuencia : int (1, 2, 3... N)",
        "DistanciaDesdeAnteriorM : decimal(10,2)",
        "Estado : varchar(30) (Ejecutado/CasaCerrada/Imposible)",
        "LecturaActual : decimal(10,2)",
        "NumeroMedidorReportado : varchar(50)",
        "FotoMedidorUrl : varchar(500)",
        "Observacion : varchar(500)",
        "GpsLatEjecucion : float",
        "GpsLonEjecucion : float"
    ]);
    addObj(diag, tAsigDetalle, 400, 630, 700, 890);

    // 9. Usuarios & Seguridad
    var tUsuarios = crearTabla(pkgERD, "Usuarios", [
        "PK IdUsuario : int",
        "Username : varchar(50) [UQ]",
        "PasswordHash : varchar(256) (PBKDF2-SHA512)",
        "Salt : varchar(128)",
        "NombreCompleto : varchar(150)",
        "IdRol : int",
        "Activo : bit"
    ]);
    addObj(diag, tUsuarios, 780, 660, 1100, 840);

    // Relaciones FK
    crearFK(tManzanas, tLotes, "1 : N (Contiene)");
    crearFK(tLotes, tConexiones, "1 : 1..N (Predio-Medidor)");
    crearFK(tTrabajadores, tAsignaciones, "1 : N (Asigna)");
    crearFK(tAsignaciones, tAsigDetalle, "1 : N (Puntos de Ruta)");
    crearFK(tConexiones, tAsigDetalle, "1 : N (Atiende)");
    crearFK(tUsuarios, tTrabajadores, "0..1 : 1 (Credencial)");

    diag.Update();
    pkgERD.Update();
    Repository.RefreshModelView(pkgERD.PackageID);
    Session.Output(">> ¡Modelo de Datos Espacial completado!");
}

function crearTabla(pkg, nombre, atributos) {
    var t as EA.Element;
    t = pkg.Elements.AddNew(nombre, "Class");
    t.Stereotype = "table";
    t.Update();

    for (var i = 0; i < atributos.length; i++) {
        var attr as EA.Attribute;
        attr = t.Attributes.AddNew(atributos[i], "");
        attr.Update();
    }
    return t;
}

function addObj(diag, elem, l, t, r, b) {
    var o as EA.DiagramObject;
    o = diag.DiagramObjects.AddNew("l=" + l + ";r=" + r + ";t=" + t + ";b=" + b + ";", "");
    o.ElementID = elem.ElementID;
    o.Update();
}

function crearFK(t1, t2, etiqueta) {
    var c as EA.Connector;
    c = t1.Connectors.AddNew(etiqueta, "Association");
    c.SupplierID = t2.ElementID;
    c.Direction = "Source -> Destination";
    c.Update();
}

main();

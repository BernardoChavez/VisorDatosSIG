!INC Local Scripts.EAConstants-JScript

/**
 * ==============================================================================
 * SISTEMA: VisorDatosSIG 2026 - UAGRM FICCT
 * SCRIPT: Generador de Diagrama de Clases del Sistema (Clean Architecture C#)
 * ==============================================================================
 */

function main() {
    Repository.EnsureOutputVisible("Script");
    Session.Output(">> Generando Diagrama de Clases del Sistema (C# / .NET 8)...");

    var targetPackage as EA.Package;
    targetPackage = Repository.GetTreeSelectedPackage();

    if (targetPackage == null) {
        Session.Prompt("Seleccione un paquete en el Project Browser.", promptOK);
        return;
    }

    var pkgClases as EA.Package;
    pkgClases = targetPackage.Packages.AddNew("2.4.1. Diagrama de Clases del Sistema", "");
    pkgClases.Update();

    var diag as EA.Diagram;
    diag = pkgClases.Diagrams.AddNew("Diagrama de Clases General VisorDatosSIG 2026", "Class");
    diag.Update();

    // 1. Interfaces y Servicios de Aplicación
    var iDijkstra = crearClase(pkgClases, "«interface» IDijkstraRoutingService", [
        "+ CalcularRutaOptima(grafo, inicio, puntos) : RutaCalculadaDto",
        "+ ConstruirGrafoDesdeRedVias(vias) : GrafoVial",
        "+ OrdenarParadasSecuencia(nodos) : List<PuntoRutaDto>"
    ]);
    addObj(diag, iDijkstra, 50, 50, 360, 170);

    var sDijkstra = crearClase(pkgClases, "DijkstraRoutingService", [
        "- _viasRepo : IViasRepository",
        "+ CalcularRutaOptima(...) : RutaCalculadaDto",
        "- EncontrarCaminoMinimo(origen, destino) : PathResult"
    ]);
    addObj(diag, sDijkstra, 50, 220, 360, 340);

    var iAguaService = crearClase(pkgClases, "«interface» IAguaService", [
        "+ ObtenerDashboardAsync() : DashboardAguaDto",
        "+ GenerarAsignacionAsync(dto) : AsignacionDto",
        "+ RegistrarResultadoCampoAsync(dto) : bool",
        "+ ObtenerTelemetriaOperariosAsync() : List<OperarioMonitoreoDto>"
    ]);
    addObj(diag, iAguaService, 420, 50, 760, 180);

    var sAguaService = crearClase(pkgClases, "AguaService", [
        "- _aguaRepo : IAguaRepository",
        "- _dijkstraService : IDijkstraRoutingService",
        "+ GenerarAsignacionAsync(dto) : AsignacionDto"
    ]);
    addObj(diag, sAguaService, 420, 220, 760, 340);

    // 2. Controladores API
    var cAguaApi = crearClase(pkgClases, "AguaApiController", [
        "- _aguaService : IAguaService",
        "+ GetDashboard() : IActionResult",
        "+ PostAsignacion(dto) : IActionResult",
        "+ PostResultadoCampo(dto) : IActionResult"
    ]);
    addObj(diag, cAguaApi, 820, 50, 1140, 180);

    var cCapasApi = crearClase(pkgClases, "CapasApiController", [
        "- _capaService : ICapaService",
        "+ GetGeoJsonBBox(capa, bbox) : IActionResult",
        "+ IdentificarEntidad(lat, lon, tol) : IActionResult"
    ]);
    addObj(diag, cCapasApi, 820, 220, 1140, 350);

    // 3. Entidades de Dominio
    var eLote = crearClase(pkgClases, "LoteEntity", [
        "+ IdLote : int",
        "+ CodigoLote : string",
        "+ Titular : string",
        "+ SuperficieM2 : decimal",
        "+ GeometriaWkt : string (Polygon)"
    ]);
    addObj(diag, eLote, 50, 420, 320, 560);

    var eConexion = crearClase(pkgClases, "ConexionAguaEntity", [
        "+ IdConexion : int",
        "+ CodigoConexion : string",
        "+ NumeroMedidor : string",
        "+ EstadoConexion : EstadoConexionEnum",
        "+ MesesMora : int",
        "+ SaldoPendiente : decimal",
        "+ Coordenada : Point"
    ]);
    addObj(diag, eConexion, 380, 420, 680, 580);

    var eAsignacion = crearClase(pkgClases, "AsignacionEntity", [
        "+ IdAsignacion : int",
        "+ CodigoAsignacion : string",
        "+ Trabajador : TrabajadorEntity",
        "+ TipoServicio : TipoServicioEnum",
        "+ RutaOptimaWkt : string",
        "+ PuntosDetalle : List<AsignacionDetalleEntity>"
    ]);
    addObj(diag, eAsignacion, 740, 420, 1080, 580);

    // Conexiones de Herencia e Implementación
    conectar(sDijkstra, iDijkstra, "implements");
    conectar(sAguaService, iAguaService, "implements");
    conectar(cAguaApi, iAguaService, "uses");
    conectar(sAguaService, iDijkstra, "uses");
    conectar(eAsignacion, eConexion, "1..* paradas");

    diag.Update();
    pkgClases.Update();
    Repository.RefreshModelView(pkgClases.PackageID);
    Session.Output(">> ¡Diagrama de Clases del Sistema completado!");
}

function crearClase(pkg, nombre, miembros) {
    var c as EA.Element;
    c = pkg.Elements.AddNew(nombre, "Class");
    c.Update();

    for (var i = 0; i < miembros.length; i++) {
        var m = miembros[i];
        if (m.indexOf("(") >= 0) {
            var meth as EA.Method;
            meth = c.Methods.AddNew(m, "");
            meth.Update();
        } else {
            var attr as EA.Attribute;
            attr = c.Attributes.AddNew(m, "");
            attr.Update();
        }
    }
    return c;
}

function addObj(diag, elem, l, t, r, b) {
    var o as EA.DiagramObject;
    o = diag.DiagramObjects.AddNew("l=" + l + ";r=" + r + ";t=" + t + ";b=" + b + ";", "");
    o.ElementID = elem.ElementID;
    o.Update();
}

function conectar(e1, e2, tipo) {
    var c as EA.Connector;
    c = e1.Connectors.AddNew("", (tipo == "implements") ? "Realisation" : "Dependency");
    c.SupplierID = e2.ElementID;
    c.Update();
}

main();

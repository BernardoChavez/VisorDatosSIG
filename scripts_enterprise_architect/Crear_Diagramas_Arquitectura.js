!INC Local Scripts.EAConstants-JScript

/**
 * ==============================================================================
 * SISTEMA: VisorDatosSIG 2026 - UAGRM FICCT
 * SCRIPT: Generador Automático de Diagramas de Arquitectura UML
 *         (Despliegue Físico, Diseño Lógico en Capas, Componentes y Paquetes)
 * MOTOR: Sparx Systems Enterprise Architect (JScript / JavaScript)
 * AUTORES: Bernardo Chávez / Miguel Angel Mendieta
 * ==============================================================================
 */

function main() {
    Repository.EnsureOutputVisible("Script");
    Session.Output(">> Iniciando generación de Diagramas de Arquitectura...");

    var targetPackage as EA.Package;
    targetPackage = Repository.GetTreeSelectedPackage();

    if (targetPackage == null) {
        Session.Prompt("Por favor seleccione un paquete en el Project Browser.", promptOK);
        return;
    }

    var pkgArch as EA.Package;
    pkgArch = targetPackage.Packages.AddNew("2.4.1. Diagramas de Arquitectura", "");
    pkgArch.Update();

    // 1. DIAGRAMA DE DESPLIEGUE FÍSICO
    Session.Output("-> Creando Diagrama de Despliegue Físico...");
    var diagDesp as EA.Diagram;
    diagDesp = pkgArch.Diagrams.AddNew("Diagrama de Despliegue Físico", "Deployment");
    diagDesp.Update();

    var nodoClienteWeb = pkgArch.Elements.AddNew("Dispositivo Cliente Web (PC/Laptop)", "Node");
    nodoClienteWeb.Update();
    var nodoClienteMovil = pkgArch.Elements.AddNew("Dispositivo Móvil (Android/iOS)", "Node");
    nodoClienteMovil.Update();
    var nodoServidorApp = pkgArch.Elements.AddNew("Servidor de Aplicaciones (Linux/Kestrel - .NET 8)", "Node");
    nodoServidorApp.Update();
    var nodoServidorBD = pkgArch.Elements.AddNew("Servidor Base de Datos (SQL Server 2022 Spatial)", "Node");
    nodoServidorBD.Update();

    addObj(diagDesp, nodoClienteWeb, 80, 100, 280, 240);
    addObj(diagDesp, nodoClienteMovil, 80, 300, 280, 440);
    addObj(diagDesp, nodoServidorApp, 420, 180, 680, 360);
    addObj(diagDesp, nodoServidorBD, 800, 180, 1040, 360);

    conectar(nodoClienteWeb, nodoServidorApp, "HTTPS / TLS 1.3 (Puerto 5000 / 443)");
    conectar(nodoClienteMovil, nodoServidorApp, "REST API / JSON (HTTP/2 - Puerto 5000)");
    conectar(nodoServidorApp, nodoServidorBD, "TDS / TCP-IP (Puerto 1433 - SRID 4326)");
    diagDesp.Update();

    // 2. DIAGRAMA DE DISEÑO LÓGICO EN CAPAS
    Session.Output("-> Creando Diagrama de Diseño Lógico en Capas...");
    var diagCapas as EA.Diagram;
    diagCapas = pkgArch.Diagrams.AddNew("Diagrama de Diseño Lógico en Capas", "Package");
    diagCapas.Update();

    var cPresentacion = pkgArch.Elements.AddNew("Capa de Presentación (Web MVC Leaflet + Mobile Flutter + Desktop WinForms)", "Package");
    cPresentacion.Update();
    var cServicios = pkgArch.Elements.AddNew("Capa de Servicios y API (ASP.NET Core Web API 8.0)", "Package");
    cServicios.Update();
    var cAplicacion = pkgArch.Elements.AddNew("Capa de Aplicación (DTOs, Interfaces, DijkstraRoutingService)", "Package");
    cAplicacion.Update();
    var cDominio = pkgArch.Elements.AddNew("Capa de Dominio (Entidades, ValueObjects, Enums, Reglas)", "Package");
    cDominio.Update();
    var cInfraestructura = pkgArch.Elements.AddNew("Capa de Infraestructura (Dapper, ShapefileReader, Repositorios)", "Package");
    cInfraestructura.Update();
    var cPersistencia = pkgArch.Elements.AddNew("Capa de Datos Espaciales (SQL Server 2022 Spatial WGS 84)", "Package");
    cPersistencia.Update();

    addObj(diagCapas, cPresentacion, 150, 50, 750, 140);
    addObj(diagCapas, cServicios, 150, 180, 750, 270);
    addObj(diagCapas, cAplicacion, 150, 310, 750, 400);
    addObj(diagCapas, cDominio, 150, 440, 750, 530);
    addObj(diagCapas, cInfraestructura, 150, 570, 750, 660);
    addObj(diagCapas, cPersistencia, 150, 700, 750, 790);

    conectar(cPresentacion, cServicios, "Usa HTTP/REST");
    conectar(cServicios, cAplicacion, "Invoca Servicios");
    conectar(cAplicacion, cDominio, "Modela Entidades");
    conectar(cAplicacion, cInfraestructura, "Implementa Interfaces");
    conectar(cInfraestructura, cPersistencia, "Consulta SQL Spatial");
    diagCapas.Update();

    // 3. DIAGRAMA DE COMPONENTES
    Session.Output("-> Creando Diagrama de Componentes...");
    var diagComp as EA.Diagram;
    diagComp = pkgArch.Diagrams.AddNew("Diagrama de Componentes del Sistema", "Component");
    diagComp.Update();

    var cmpWebUI = pkgArch.Elements.AddNew("VisorDatosSIG.Web (Leaflet.js + Bootstrap 5)", "Component");
    cmpWebUI.Update();
    var cmpApiController = pkgArch.Elements.AddNew("VisorDatosSIG.Api (Controllers)", "Component");
    cmpApiController.Update();
    var cmpDijkstra = pkgArch.Elements.AddNew("DijkstraRoutingService (Rutas en Red Vial)", "Component");
    cmpDijkstra.Update();
    var cmpShpParser = pkgArch.Elements.AddNew("ShapefileParser (Lector Binario ESRI)", "Component");
    cmpShpParser.Update();
    var cmpSpatialRepo = pkgArch.Elements.AddNew("SpatialRepository (Consultas BBox / STIntersects)", "Component");
    cmpSpatialRepo.Update();
    var cmpDb = pkgArch.Elements.AddNew("SQL Server 2022 Spatial (SRID 4326)", "Component");
    cmpDb.Update();

    addObj(diagComp, cmpWebUI, 80, 100, 320, 200);
    addObj(diagComp, cmpApiController, 420, 100, 680, 200);
    addObj(diagComp, cmpDijkstra, 420, 260, 680, 360);
    addObj(diagComp, cmpShpParser, 420, 420, 680, 520);
    addObj(diagComp, cmpSpatialRepo, 780, 200, 1020, 300);
    addObj(diagComp, cmpDb, 780, 380, 1020, 480);

    conectar(cmpWebUI, cmpApiController, "HTTP/JSON");
    conectar(cmpApiController, cmpDijkstra, "Usa");
    conectar(cmpApiController, cmpSpatialRepo, "Usa");
    conectar(cmpSpatialRepo, cmpDb, "TCP 1433");
    diagComp.Update();

    pkgArch.Update();
    Repository.RefreshModelView(pkgArch.PackageID);
    Session.Output(">> ¡Generación de Diagramas de Arquitectura finalizada con éxito!");
    Session.Prompt("Se crearon exitosamente los Diagramas de Arquitectura.", promptOK);
}

function addObj(diag, elem, l, t, r, b) {
    var o as EA.DiagramObject;
    o = diag.DiagramObjects.AddNew("l=" + l + ";r=" + r + ";t=" + t + ";b=" + b + ";", "");
    o.ElementID = elem.ElementID;
    o.Update();
}

function conectar(e1, e2, desc) {
    var c as EA.Connector;
    c = e1.Connectors.AddNew(desc, "Dependency");
    c.SupplierID = e2.ElementID;
    c.Update();
}

main();

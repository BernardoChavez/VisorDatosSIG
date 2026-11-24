!INC Local Scripts.EAConstants-JScript

/**
 * ==============================================================================
 * SISTEMA: VisorDatosSIG 2026 - UAGRM FICCT
 * SCRIPT: Generador de Diagramas de Diseño Lógico en Capas y Componentes Internos
 *         (Secciones 2.4.1.2 y 2.4.1.3 del Documento de Software)
 * ==============================================================================
 */

function main() {
    Repository.EnsureOutputVisible("Script");
    Session.Output(">> Generando Diseño Lógico en Capas y Componentes Internos...");

    var targetPackage as EA.Package;
    targetPackage = Repository.GetTreeSelectedPackage();

    if (targetPackage == null) {
        Session.Prompt("Seleccione un paquete en el Project Browser.", promptOK);
        return;
    }

    var pkgDiseño as EA.Package;
    pkgDiseño = targetPackage.Packages.AddNew("2.4.1. Arquitectura Logica y Componentes", "");
    pkgDiseño.Update();

    // ==========================================================================
    // 1. DIAGRAMA 2.4.1.2: DISEÑO LÓGICO EN CAPAS (CLEAN ARCHITECTURE)
    // ==========================================================================
    Session.Output("-> 1. Creando Diagrama de Diseño Lógico en Capas (2.4.1.2)...");
    var diagCapas as EA.Diagram;
    diagCapas = pkgDiseño.Diagrams.AddNew("2.4.1.2. Diagrama de Diseño Lógico en Capas", "Package");
    diagCapas.Update();

    // Capa 1: Presentación
    var pPres = pkgDiseño.Elements.AddNew("Capa de Presentación (UI)\n- VisorDatosSIG.Web (ASP.NET Core MVC + Leaflet.js)\n- VisorDatosSIG.Movil (Flutter Android/iOS)\n- VisorDatosSIG.Migrador (WinForms .NET 8)", "Package");
    pPres.Update();
    addObj(diagCapas, pPres, 100, 60, 850, 160);

    // Capa 2: Aplicación
    var pApp = pkgDiseño.Elements.AddNew("Capa de Aplicación (Application)\n- Casos de Uso & DTOs (Data Transfer Objects)\n- Interfaces (IAguaService, ICapaService, IUsuarioService)\n- DijkstraRoutingService (Algoritmo de Ruteo sobre Red Vial)", "Package");
    pApp.Update();
    addObj(diagCapas, pApp, 100, 210, 850, 310);

    // Capa 3: Dominio
    var pDom = pkgDiseño.Elements.AddNew("Capa de Dominio (Domain)\n- Entidades Puras (Lote, Manzana, Via, ConexionAgua, Asignacion)\n- Objetos de Valor & Enums (EstadoConexion, TipoTrabajador)\n- Reglas y Validaciones de Negocio", "Package");
    pDom.Update();
    addObj(diagCapas, pDom, 100, 360, 850, 460);

    // Capa 4: Infraestructura
    var pInfra = pkgDiseño.Elements.AddNew("Capa de Infraestructura (Infrastructure)\n- Repositorios Dapper (AguaRepository, CapaRepository)\n- ShapefileReader & NetTopologySuite (Parser Binario ESRI)\n- Acceso Criptográfico PBKDF2-HMAC-SHA512", "Package");
    pInfra.Update();
    addObj(diagCapas, pInfra, 100, 510, 850, 610);

    // Capa 5: Persistencia
    var pPers = pkgDiseño.Elements.AddNew("Capa de Datos Espaciales (Database)\n- Microsoft SQL Server 2022 Spatial (SRID 4326)\n- Columnas Tipo GEOMETRY WGS 84 e Índices R-Tree Grid\n- Stored Procedures y Bitácora de Auditoría Inmutable", "Package");
    pPers.Update();
    addObj(diagCapas, pPers, 100, 660, 850, 760);

    // Dependencias entre capas (Clean Architecture: flujo hacia abajo)
    conectar(pPres, pApp, "Consume DTOs y Servicios");
    conectar(pApp, pDom, "Modela Entidades y Reglas");
    conectar(pInfra, pDom, "Mapea Entidades");
    conectar(pInfra, pApp, "Implementa Interfaces");
    conectar(pInfra, pPers, "Consultas T-SQL Spatial");
    diagCapas.Update();

    // ==========================================================================
    // 2. DIAGRAMA 2.4.1.3: DISEÑO INTERNO DE COMPONENTES DEL SISTEMA
    // ==========================================================================
    Session.Output("-> 2. Creando Diseño Interno de Componentes (2.4.1.3)...");
    var diagComp as EA.Diagram;
    diagComp = pkgDiseño.Diagrams.AddNew("2.4.1.3. Diseño Interno de Componentes del Sistema", "Component");
    diagComp.Update();

    // Fila 1: Controllers
    var cCapasApi = pkgDiseño.Elements.AddNew("«component»\nCapasApiController\n(Endpoints REST GeoJSON BBox)", "Component"); cCapasApi.Update();
    var cAguaApi = pkgDiseño.Elements.AddNew("«component»\nAguaApiController\n(Dashboard, Asignaciones, Móvil)", "Component"); cAguaApi.Update();
    var cBusqApi = pkgDiseño.Elements.AddNew("«component»\nBusquedaApiController\n(Búsqueda Multiatributo)", "Component"); cBusqApi.Update();
    var cAdmin = pkgDiseño.Elements.AddNew("«component»\nAdminController\n(Usuarios, PBAC, Auditoría)", "Component"); cAdmin.Update();

    addObj(diagComp, cCapasApi, 50, 60, 270, 160);
    addObj(diagComp, cAguaApi, 320, 60, 560, 160);
    addObj(diagComp, cBusqApi, 610, 60, 850, 160);
    addObj(diagComp, cAdmin, 900, 60, 1140, 160);

    // Fila 2: Services
    var sCapa = pkgDiseño.Elements.AddNew("«component»\nCapaService\n(Procesamiento GeoJSON RFC 7946)", "Component"); sCapa.Update();
    var sAgua = pkgDiseño.Elements.AddNew("«component»\nAguaService\n(Gestión Cuadrillas y Cortes)", "Component"); sAgua.Update();
    var sDijkstra = pkgDiseño.Elements.AddNew("«component»\nDijkstraRoutingService\n(Algoritmo Grafo Red Vial)", "Component"); sDijkstra.Update();
    var sAudit = pkgDiseño.Elements.AddNew("«component»\nAuditoriaService\n(Seguridad y Trazabilidad)", "Component"); sAudit.Update();

    addObj(diagComp, sCapa, 50, 240, 270, 340);
    addObj(diagComp, sAgua, 320, 240, 560, 340);
    addObj(diagComp, sDijkstra, 610, 240, 850, 340);
    addObj(diagComp, sAudit, 900, 240, 1140, 340);

    // Fila 3: Repositories
    var rCapa = pkgDiseño.Elements.AddNew("«component»\nCapaRepository\n(Consultas STIntersects / BBox)", "Component"); rCapa.Update();
    var rAgua = pkgDiseño.Elements.AddNew("«component»\nAguaRepository\n(Persistencia Órdenes y Lecturas)", "Component"); rAgua.Update();
    var rVias = pkgDiseño.Elements.AddNew("«component»\nViasRepository\n(Extracción de Aristas Viales)", "Component"); rVias.Update();
    var rBitacora = pkgDiseño.Elements.AddNew("«component»\nBitacoraRepository\n(Inserción Inmutable de Logs)", "Component"); rBitacora.Update();

    addObj(diagComp, rCapa, 50, 420, 270, 520);
    addObj(diagComp, rAgua, 320, 420, 560, 520);
    addObj(diagComp, rVias, 610, 420, 850, 520);
    addObj(diagComp, rBitacora, 900, 420, 1140, 520);

    // Fila 4: SpatialEngine y Motor BD
    var cmpShp = pkgDiseño.Elements.AddNew("«component»\nShapefileParser / NTS\n(Lector Binario .shp/.prj)", "Component"); cmpShp.Update();
    var cmpEngine = pkgDiseño.Elements.AddNew("«component»\nSpatialEngine\n(Motor Geométrico NetTopologySuite)", "Component"); cmpEngine.Update();
    var cmpDb = pkgDiseño.Elements.AddNew("«database»\nSQL Server 2022 Spatial\n(GEOMETRY SRID 4326)", "Component"); cmpDb.Update();

    addObj(diagComp, cmpShp, 50, 600, 320, 700);
    addObj(diagComp, cmpEngine, 400, 600, 740, 700);
    addObj(diagComp, cmpDb, 820, 600, 1140, 700);

    // Conexiones de dependencia interna
    // Controllers -> Services
    conectar(cCapasApi, sCapa, "«use»");
    conectar(cAguaApi, sAgua, "«use»");
    conectar(cAguaApi, sDijkstra, "«use»");
    conectar(cBusqApi, sCapa, "«use»");
    conectar(cAdmin, sAudit, "«use»");

    // Services -> Services / Repositories
    conectar(sAgua, sDijkstra, "«use»");
    conectar(sCapa, rCapa, "«use»");
    conectar(sAgua, rAgua, "«use»");
    conectar(sDijkstra, rVias, "«use»");
    conectar(sAudit, rBitacora, "«use»");

    // Repositories -> SpatialEngine / DB
    conectar(rCapa, cmpEngine, "Valida OGC");
    conectar(rCapa, cmpDb, "TDS 1433");
    conectar(rAgua, cmpDb, "TDS 1433");
    conectar(rVias, cmpDb, "TDS 1433");
    conectar(rBitacora, cmpDb, "TDS 1433");
    conectar(cmpShp, cmpEngine, "Convierte WKT");

    diagComp.Update();

    pkgDiseño.Update();
    Repository.RefreshModelView(pkgDiseño.PackageID);
    Session.Output(">> ¡Diseño Lógico en Capas y Componentes completados exitosamente!");
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

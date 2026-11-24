!INC Local Scripts.EAConstants-JScript

/**
 * ==============================================================================
 * SISTEMA: VisorDatosSIG 2026 - UAGRM FICCT
 * SCRIPT: Generador de Diagramas de Paquetes de Análisis
 *         - Sección 2.3.3: Diagrama de Dependencia entre Paquetes
 *         - Sección 2.3.1.2: Diagrama de Trazabilidad Paquetes y Casos de Uso
 * ==============================================================================
 */

function main() {
    Repository.EnsureOutputVisible("Script");
    Session.Output(">> Generando Diagramas de Paquetes de Análisis (2.3.1.2 y 2.3.3)...");

    var targetPackage as EA.Package;
    targetPackage = Repository.GetTreeSelectedPackage();

    if (targetPackage == null) {
        Session.Prompt("Seleccione un paquete en el Project Browser.", promptOK);
        return;
    }

    var pkgAnalisis as EA.Package;
    pkgAnalisis = targetPackage.Packages.AddNew("2.3. Analisis de Paquetes", "");
    pkgAnalisis.Update();

    // ==========================================================================
    // 1. DIAGRAMA 2.3.3: DEPENDENCIA ENTRE PAQUETES DE ANÁLISIS
    // ==========================================================================
    Session.Output("-> 1. Creando Diagrama de Dependencia entre Paquetes (2.3.3)...");
    var diagDep as EA.Diagram;
    diagDep = pkgAnalisis.Diagrams.AddNew("2.3.3. Diagrama de Dependencia entre Paquetes", "Package");
    diagDep.Update();

    // Crear Elementos Tipo 'Package'
    // Fila 1: Presentación / Interacción de Usuario y Migración
    var pVisor = pkgAnalisis.Elements.AddNew("pkg_Visor_Cartografico\n- Lienzo Leaflet interactivo\n- Renderizado de capas vectoriales\n- Selector de mapas base (OSM/Satelital)\n- Herramientas de visualización y leyendas", "Package");
    pVisor.Update();
    addObj(diagDep, pVisor, 60, 60, 420, 200);

    var pConsultas = pkgAnalisis.Elements.AddNew("pkg_Consultas_Exportacion\n- Búsqueda predictiva debounce\n- Mediciones de distancia y polígonos\n- Geolocalización GPS y coordenadas\n- Exportación CSV (logs) y WKT (geometría)", "Package");
    pConsultas.Update();
    addObj(diagDep, pConsultas, 480, 60, 840, 200);

    var pMigrador = pkgAnalisis.Elements.AddNew("pkg_Migrador_Espacial\n- Inspección de SHP / DBF / PRJ\n- Validación topológica STIsValid()\n- Transformación WGS 84 (SRID 4326)\n- Carga masiva transaccional por lotes", "Package");
    pMigrador.Update();
    addObj(diagDep, pMigrador, 900, 60, 1260, 200);

    // Fila 2: Lógica de Negocio y Servicios de Integración
    var pAgua = pkgAnalisis.Elements.AddNew("pkg_Gestion_Agua_Dijkstra\n- Conexiones y detección de mora\n- Órdenes de corte y reconexión\n- Grafo vial y camino mínimo Dijkstra\n- Telemetría móvil de cuadrillas en campo", "Package");
    pAgua.Update();
    addObj(diagDep, pAgua, 160, 280, 560, 420);

    var pApi = pkgAnalisis.Elements.AddNew("pkg_Servicios_API_GeoJSON\n- Endpoints RESTful GeoJSON (RFC 7946)\n- Filtrado espacial por Bounding Box\n- Consultas espaciales (STIntersects)\n- Catálogo de capas y metadatos", "Package");
    pApi.Update();
    addObj(diagDep, pApi, 760, 280, 1160, 420);

    // Fila 3: Soporte Transversal y Seguridad
    var pSeg = pkgAnalisis.Elements.AddNew("pkg_Seguridad_Acceso\n- Autenticación y gestión de sesiones\n- Hashing PBKDF2-HMAC-SHA512 con Salt\n- Control de acceso por roles (RBAC/PBAC)\n- Bitácora inmutable de auditoría del sistema", "Package");
    pSeg.Update();
    addObj(diagDep, pSeg, 460, 500, 860, 640);

    // Conectores de Dependencia («use»)
    conectar(pVisor, pApi, "«use» Consume GeoJSON");
    conectar(pVisor, pConsultas, "«use» Herramientas y medidas");
    conectar(pVisor, pAgua, "«use» Muestra rutas y cortes");

    conectar(pConsultas, pApi, "«use» Atributos y BBox");

    conectar(pAgua, pApi, "«use» Red vial y conexiones");
    conectar(pAgua, pSeg, "«use» Valida cuadrilla");

    conectar(pMigrador, pSeg, "«use» Rol Administrador");
    conectar(pMigrador, pApi, "«use» Catálogo capas");

    conectar(pApi, pSeg, "«use» Valida y audita");

    diagDep.Update();

    // ==========================================================================
    // 2. DIAGRAMA 2.3.1.2: TRAZABILIDAD ENTRE PAQUETES Y CASOS DE USO
    // ==========================================================================
    Session.Output("-> 2. Creando Diagrama de Trazabilidad Paquetes y Casos de Uso (2.3.1.2)...");
    var diagTraza as EA.Diagram;
    diagTraza = pkgAnalisis.Diagrams.AddNew("2.3.1.2. Trazabilidad Paquetes y Casos de Uso", "Use Case");
    diagTraza.Update();

    // Paquetes y Casos de Uso
    addObj(diagTraza, pSeg, 60, 60, 360, 180);
    var cu01 = pkgAnalisis.Elements.AddNew("CU01: Gestión de Sesión", "UseCase"); cu01.Update();
    var cu02 = pkgAnalisis.Elements.AddNew("CU02: Gestión de Usuarios", "UseCase"); cu02.Update();
    var cu03 = pkgAnalisis.Elements.AddNew("CU03: Bitácora y Auditoría", "UseCase"); cu03.Update();
    addObj(diagTraza, cu01, 60, 240, 200, 300);
    addObj(diagTraza, cu02, 220, 240, 360, 300);
    addObj(diagTraza, cu03, 140, 330, 280, 390);
    conectarTraza(pSeg, cu01);
    conectarTraza(pSeg, cu02);
    conectarTraza(pSeg, cu03);

    addObj(diagTraza, pMigrador, 440, 60, 740, 180);
    var cu05 = pkgAnalisis.Elements.AddNew("CU05: Inspeccionar Shapefiles", "UseCase"); cu05.Update();
    var cu06 = pkgAnalisis.Elements.AddNew("CU06: Validar WGS 84 / SRID 4326", "UseCase"); cu06.Update();
    var cu08 = pkgAnalisis.Elements.AddNew("CU08: Migración Masiva Lotes", "UseCase"); cu08.Update();
    addObj(diagTraza, cu05, 440, 240, 580, 300);
    addObj(diagTraza, cu06, 600, 240, 740, 300);
    addObj(diagTraza, cu08, 520, 330, 660, 390);
    conectarTraza(pMigrador, cu05);
    conectarTraza(pMigrador, cu06);
    conectarTraza(pMigrador, cu08);

    addObj(diagTraza, pVisor, 820, 60, 1120, 180);
    var cu11 = pkgAnalisis.Elements.AddNew("CU11: Visualizar Capas y Mapas", "UseCase"); cu11.Update();
    var cu13 = pkgAnalisis.Elements.AddNew("CU13: Identificar por Clic", "UseCase"); cu13.Update();
    var cu18 = pkgAnalisis.Elements.AddNew("CU18: Navegación Móvil", "UseCase"); cu18.Update();
    addObj(diagTraza, cu11, 820, 240, 960, 300);
    addObj(diagTraza, cu13, 980, 240, 1120, 300);
    addObj(diagTraza, cu18, 900, 330, 1040, 390);
    conectarTraza(pVisor, cu11);
    conectarTraza(pVisor, cu13);
    conectarTraza(pVisor, cu18);

    addObj(diagTraza, pConsultas, 60, 450, 360, 570);
    var cu14 = pkgAnalisis.Elements.AddNew("CU14: Búsqueda Predictiva", "UseCase"); cu14.Update();
    var cu15 = pkgAnalisis.Elements.AddNew("CU15: Medición Cartográfica", "UseCase"); cu15.Update();
    var cu16 = pkgAnalisis.Elements.AddNew("CU16: Geolocalización GPS", "UseCase"); cu16.Update();
    var cu17 = pkgAnalisis.Elements.AddNew("CU17: Exportación WKT y Logs", "UseCase"); cu17.Update();
    addObj(diagTraza, cu14, 60, 620, 200, 680);
    addObj(diagTraza, cu15, 220, 620, 360, 680);
    addObj(diagTraza, cu16, 60, 710, 200, 770);
    addObj(diagTraza, cu17, 220, 710, 360, 770);
    conectarTraza(pConsultas, cu14);
    conectarTraza(pConsultas, cu15);
    conectarTraza(pConsultas, cu16);
    conectarTraza(pConsultas, cu17);

    addObj(diagTraza, pAgua, 440, 450, 740, 570);
    var cu19 = pkgAnalisis.Elements.AddNew("CU19: Conexiones y Mora", "UseCase"); cu19.Update();
    var cu20 = pkgAnalisis.Elements.AddNew("CU20: Órdenes de Corte", "UseCase"); cu20.Update();
    var cu21 = pkgAnalisis.Elements.AddNew("CU21: Ruteo Vial Dijkstra", "UseCase"); cu21.Update();
    var cu22 = pkgAnalisis.Elements.AddNew("CU22: Telemetría de Campo", "UseCase"); cu22.Update();
    addObj(diagTraza, cu19, 440, 620, 580, 680);
    addObj(diagTraza, cu20, 600, 620, 740, 680);
    addObj(diagTraza, cu21, 440, 710, 580, 770);
    addObj(diagTraza, cu22, 600, 710, 740, 770);
    conectarTraza(pAgua, cu19);
    conectarTraza(pAgua, cu20);
    conectarTraza(pAgua, cu21);
    conectarTraza(pAgua, cu22);

    addObj(diagTraza, pApi, 820, 450, 1120, 570);
    var cu09 = pkgAnalisis.Elements.AddNew("CU09: Catálogo de Capas", "UseCase"); cu09.Update();
    var cu10 = pkgAnalisis.Elements.AddNew("CU10: GeoJSON Filtrado BBox", "UseCase"); cu10.Update();
    var cu12 = pkgAnalisis.Elements.AddNew("CU12: Buffer y Topología", "UseCase"); cu12.Update();
    addObj(diagTraza, cu09, 820, 620, 960, 680);
    addObj(diagTraza, cu10, 980, 620, 1120, 680);
    addObj(diagTraza, cu12, 900, 710, 1040, 770);
    conectarTraza(pApi, cu09);
    conectarTraza(pApi, cu10);
    conectarTraza(pApi, cu12);

    diagTraza.Update();

    pkgAnalisis.Update();
    Repository.RefreshModelView(pkgAnalisis.PackageID);
    Session.Output(">> ¡Diagramas de Paquetes generados con éxito!");
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

function conectarTraza(pkg, cu) {
    var c as EA.Connector;
    c = pkg.Connectors.AddNew("«trace»", "Dependency");
    c.SupplierID = cu.ElementID;
    c.Update();
}

main();

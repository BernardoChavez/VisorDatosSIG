!INC Local Scripts.EAConstants-JScript

/**
 * ==============================================================================
 * SISTEMA: VisorDatosSIG 2026 - UAGRM FICCT
 * SCRIPT: Generador de Diagramas de Arquitectura y Despliegue (Estilo Bloques UAGRM)
 * ==============================================================================
 */

function main() {
    Repository.EnsureOutputVisible("Script");
    Session.Output(">> Generando Diagramas de Arquitectura y Despliegue...");

    var targetPackage as EA.Package;
    targetPackage = Repository.GetTreeSelectedPackage();

    if (targetPackage == null) {
        Session.Prompt("Seleccione un paquete en el Project Browser.", promptOK);
        return;
    }

    var pkgArch as EA.Package;
    pkgArch = targetPackage.Packages.AddNew("2.4.1. Diagramas de Arquitectura y Despliegue", "");
    pkgArch.Update();

    // --------------------------------------------------------------------------
    // 1. DIAGRAMA DE ARQUITECTURA POR BLOQUES (Como Imagen 3 de referencia)
    // --------------------------------------------------------------------------
    Session.Output("-> 1. Diagrama de Arquitectura Modular por Bloques...");
    var diagArq as EA.Diagram;
    diagArq = pkgArch.Diagrams.AddNew("2.1.7.3. Arquitectura del Sistema VisorDatosSIG", "Component");
    diagArq.Update();

    // Bloque 1: Cliente Web
    var bClienteWeb = pkgArch.Elements.AddNew("«browser»\nCliente Web VisorDatosSIG\n(Consultores y Administradores)", "Component");
    bClienteWeb.Update();
    addObj(diagArq, bClienteWeb, 50, 80, 320, 350);

    // Sub-bloques Cliente Web
    var sw1 = pkgArch.Elements.AddNew("Leaflet.js 1.9 + Bootstrap 5", "Component"); sw1.Update();
    var sw2 = pkgArch.Elements.AddNew("Interfaz Cartográfica & HUD Coordenadas", "Component"); sw2.Update();
    var sw3 = pkgArch.Elements.AddNew("Módulos JS: Map, Layers, Measure, Search", "Component"); sw3.Update();
    addObj(diagArq, sw1, 70, 150, 300, 200);
    addObj(diagArq, sw2, 70, 215, 300, 265);
    addObj(diagArq, sw3, 70, 280, 300, 330);

    // Bloque 2: Backend Principal
    var bBackend = pkgArch.Elements.AddNew("Backend Principal VisorDatosSIG\n(ASP.NET Core 8 Web API & MVC)", "Component");
    bBackend.Update();
    addObj(diagArq, bBackend, 380, 80, 720, 540);

    var sb1 = pkgArch.Elements.AddNew("Kestrel / Reverse Proxy HTTPS (Puerto 5000)", "Component"); sb1.Update();
    var sb2 = pkgArch.Elements.AddNew("Controladores: Capas, Agua, Admin, Busqueda", "Component"); sb2.Update();
    var sb3 = pkgArch.Elements.AddNew("Autenticación PBKDF2-HMAC-SHA512 + PBAC", "Component"); sb3.Update();
    var sb4 = pkgArch.Elements.AddNew("Módulos Negocio: Catastro, Red Vial, Cortes/Reconexiones", "Component"); sb4.Update();
    var sb5 = pkgArch.Elements.AddNew("DijkstraRoutingService (Grafo Red de Calles)", "Component"); sb5.Update();
    var sb6 = pkgArch.Elements.AddNew("Dapper ORM & Spatial Repositories", "Component"); sb6.Update();
    var sb7 = pkgArch.Elements.AddNew("Microsoft SQL Server 2022 Spatial (SRID 4326)", "Component"); sb7.Update();

    addObj(diagArq, sb1, 400, 140, 700, 185);
    addObj(diagArq, sb2, 400, 195, 700, 240);
    addObj(diagArq, sb3, 400, 250, 700, 295);
    addObj(diagArq, sb4, 400, 305, 700, 350);
    addObj(diagArq, sb5, 400, 360, 700, 405);
    addObj(diagArq, sb6, 400, 415, 700, 460);
    addObj(diagArq, sb7, 400, 470, 700, 520);

    // Bloque 3: Módulo Móvil Cuadrillas
    var bMovil = pkgArch.Elements.AddNew("«device»\nApp Móvil Cuadrillas\n(Flutter / Dart)", "Component");
    bMovil.Update();
    addObj(diagArq, bMovil, 780, 80, 1050, 350);

    var sm1 = pkgArch.Elements.AddNew("Flutter UI (TaskList, RouteMap, FieldRecord)", "Component"); sm1.Update();
    var sm2 = pkgArch.Elements.AddNew("Módulo GPS & Telemetría en Vivo", "Component"); sm2.Update();
    var sm3 = pkgArch.Elements.AddNew("Cámara y Captura de Evidencias de Medidor", "Component"); sm3.Update();
    addObj(diagArq, sm1, 800, 150, 1030, 200);
    addObj(diagArq, sm2, 800, 215, 1030, 265);
    addObj(diagArq, sm3, 800, 280, 1030, 330);

    // Bloque 4: Suite de Escritorio ETL
    var bDesktop = pkgArch.Elements.AddNew("Suite Escritorio ETL\n(VisorDatosSIG.Migrador)", "Component");
    bDesktop.Update();
    addObj(diagArq, bDesktop, 50, 400, 320, 590);

    var sd1 = pkgArch.Elements.AddNew("Parser Binario Shapefile (.shp/.shx/.dbf/.prj)", "Component"); sd1.Update();
    var sd2 = pkgArch.Elements.AddNew("Validador Criptográfico WGS 84 (SRID 4326)", "Component"); sd2.Update();
    var sd3 = pkgArch.Elements.AddNew("Motor Transaccional por Lotes (Batch Insert)", "Component"); sd3.Update();
    addObj(diagArq, sd1, 70, 450, 300, 490);
    addObj(diagArq, sd2, 70, 495, 300, 535);
    addObj(diagArq, sd3, 70, 540, 300, 580);

    // Conectores con protocolos
    conectar(bClienteWeb, bBackend, "HTTPS REST / GeoJSON RFC 7946");
    conectar(bMovil, bBackend, "REST API JSON / HTTP 2 (Puerto 5000)");
    conectar(bDesktop, bBackend, "TDS / TCP-IP (Puerto 1433 - Batch)");
    diagArq.Update();

    // --------------------------------------------------------------------------
    // 2. DIAGRAMA DE DESPLIEGUE FÍSICO (Como Imagen 4 de referencia)
    // --------------------------------------------------------------------------
    Session.Output("-> 2. Diagrama de Despliegue Físico Jerárquico...");
    var diagDesp as EA.Diagram;
    diagDesp = pkgArch.Diagrams.AddNew("Figura 3. Diagrama de Despliegue - VisorDatosSIG 2026", "Deployment");
    diagDesp.Update();

    // Nodo 1: Servidor de BD
    var nBD = pkgArch.Elements.AddNew("Servidor Base de Datos Espacial\n«execution environment»\nUbuntu Server 22.04 LTS / Docker Host", "Node"); nBD.Update();
    var aBD = pkgArch.Elements.AddNew("Motor Persistencia: SQL Server 2022 Spatial\n(GEOMETRY WGS 84, Tablas, R-Tree Indexes)", "Artifact"); aBD.Update();
    addObj(diagDesp, nBD, 50, 50, 380, 260);
    addObj(diagDesp, aBD, 70, 140, 360, 240);

    // Nodo 2: Servidor de Aplicaciones
    var nApp = pkgArch.Elements.AddNew("Servidor de Aplicaciones Web & API\n«execution environment»\nLinux Kestrel Runtime .NET 8.0", "Node"); nApp.Update();
    var aApp = pkgArch.Elements.AddNew("VisorDatosSIG.Web.dll & VisorDatosSIG.Api.dll\n(REST Services, Dijkstra Engine, Dapper)", "Artifact"); aApp.Update();
    addObj(diagDesp, nApp, 450, 50, 780, 260);
    addObj(diagDesp, aApp, 470, 140, 760, 240);

    // Nodo 3: Servidores Base Cartográficos (Cloud)
    var nCloud = pkgArch.Elements.AddNew("Servidores Cartográficos Base (Nube)\n«cloud environment»\nOSM Tile Server / Carto CDN / Esri Imagery", "Node"); nCloud.Update();
    addObj(diagDesp, nCloud, 850, 50, 1150, 200);

    // Nodo 4: PC Cliente Web
    var nPCWeb = pkgArch.Elements.AddNew("«device»\nPC / Laptop del Consultor Catastral\n«execution environment» Windows 10/11 - Chrome/Edge", "Node"); nPCWeb.Update();
    var aBrowser = pkgArch.Elements.AddNew("Navegador Web: Leaflet.js 1.9 SPA\n(Lienzo Cartográfico, HUD, Herramientas)", "Artifact"); aBrowser.Update();
    addObj(diagDesp, nPCWeb, 50, 340, 380, 550);
    addObj(diagDesp, aBrowser, 70, 430, 360, 530);

    // Nodo 5: PC Operador Migrador
    var nPCMigrador = pkgArch.Elements.AddNew("«device»\nPC Operador de Datos Espaciales\n«execution environment» Windows 10/11 x64 (.NET 8)", "Node"); nPCMigrador.Update();
    var aMigrador = pkgArch.Elements.AddNew("VisorDatosSIG.Migrador.exe\n(Lector Shapefiles Nativo)", "Artifact"); aMigrador.Update();
    addObj(diagDesp, nPCMigrador, 450, 340, 780, 550);
    addObj(diagDesp, aMigrador, 470, 430, 760, 530);

    // Nodo 6: Móvil Cuadrilla
    var nSmartphone = pkgArch.Elements.AddNew("«device»\nSmartphone Cuadrilla de Terreno\n«execution environment» Android 10+ / iOS 14+", "Node"); nSmartphone.Update();
    var aApk = pkgArch.Elements.AddNew("VisorDatosSIG_Movil.apk\n(Flutter Engine, GPS Tracker, Cámara)", "Artifact"); aApk.Update();
    addObj(diagDesp, nSmartphone, 850, 340, 1150, 550);
    addObj(diagDesp, aApk, 870, 430, 1130, 530);

    // Conexiones de Despliegue
    conectar(nApp, nBD, "TDS / TCP-IP (Puerto 1433 - SRID 4326)");
    conectar(nPCWeb, nApp, "HTTPS / TLS 1.3 (Puerto 5000 / 443)");
    conectar(nPCWeb, nCloud, "HTTP/S (Tile Request XYZ)");
    conectar(nPCMigrador, nBD, "TCP-IP 1433 (Transaccional Batch)");
    conectar(nSmartphone, nApp, "REST API JSON / HTTP 2.0 (Telemetría)");
    diagDesp.Update();

    pkgArch.Update();
    Repository.RefreshModelView(pkgArch.PackageID);
    Session.Output(">> ¡Diagramas de Arquitectura y Despliegue completados!");
}

function addObj(diag, elem, l, t, r, b) {
    var o as EA.DiagramObject;
    o = diag.DiagramObjects.AddNew("l=" + l + ";r=" + r + ";t=" + t + ";b=" + b + ";", "");
    o.ElementID = elem.ElementID;
    o.Update();
}

function conectar(e1, e2, desc) {
    var c as EA.Connector;
    c = e1.Connectors.AddNew(desc, "CommunicationPath");
    c.SupplierID = e2.ElementID;
    c.Update();
}

main();

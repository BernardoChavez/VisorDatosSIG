!INC Local Scripts.EAConstants-JScript

/**
 * ==============================================================================
 * SISTEMA: VisorDatosSIG 2026 - UAGRM FICCT
 * SCRIPT: Generador de Diagramas de Comunicación (Robustez BCE Estilo UAGRM)
 * ==============================================================================
 */

function main() {
    Repository.EnsureOutputVisible("Script");
    Session.Output(">> Generando Diagramas de Comunicación (Estilo Robustez BCE)...");

    var targetPackage as EA.Package;
    targetPackage = Repository.GetTreeSelectedPackage();

    if (targetPackage == null) {
        Session.Prompt("Seleccione un paquete en el Project Browser.", promptOK);
        return;
    }

    var pkgCom as EA.Package;
    pkgCom = targetPackage.Packages.AddNew("2.3.2. Diagramas de Comunicación", "");
    pkgCom.Update();

    var casos = [
        {
            codigo: "CU01",
            titulo: "CU01 - Gestión de Sesión y Autenticación",
            actor: "Usuario / Administrador",
            boundary: "UI_LoginView",
            control: "AutenticacionController",
            entity: "Usuario",
            mensajes: [
                { de: "actor", a: "boundary", texto: "1.1: ingresarCredenciales(user, pass) ->" },
                { de: "boundary", a: "control", texto: "1.2: validarCredenciales(dto) ->" },
                { de: "control", a: "entity", texto: "1.3: -> buscarPorUsername(user)" },
                { de: "entity", a: "control", texto: "1.4: <- retornarHashYSalt(salt, hash)" },
                { de: "control", a: "control", texto: "1.5: verificarPBKDF2(pass, hash)" },
                { de: "control", a: "boundary", texto: "1.6: <- emitirCookieSesion(Claims)" },
                { de: "boundary", a: "actor", texto: "1.7: <- redireccionarAlVisor()" }
            ]
        },
        {
            codigo: "CU02",
            titulo: "CU02 - Gestión de Usuarios y Permisos PBAC",
            actor: "Administrador",
            boundary: "UI_GestionUsuarios",
            control: "AdminController",
            entity: "UsuarioPermiso",
            mensajes: [
                { de: "actor", a: "boundary", texto: "1.1: solicitarListaUsuarios() ->" },
                { de: "boundary", a: "control", texto: "1.2: obtenerUsuariosYPermisos() ->" },
                { de: "control", a: "entity", texto: "1.3: -> consultarPermisosPBAC(id)" },
                { de: "boundary", a: "control", texto: "2.1: guardarPermisos(id, lista) ->" },
                { de: "control", a: "entity", texto: "2.2: UPDATE UsuarioPermisos ->" },
                { de: "control", a: "boundary", texto: "2.3: <- notificarExito()" }
            ]
        },
        {
            codigo: "CU05",
            titulo: "CU05 - Seleccionar e Inspeccionar Archivos Shapefile",
            actor: "Operador Migrador",
            boundary: "UI_MainMigradorForm",
            control: "MigradorService",
            entity: "ShapefileHeader",
            mensajes: [
                { de: "actor", a: "boundary", texto: "1.1: seleccionarRutaDirectorio() ->" },
                { de: "boundary", a: "control", texto: "1.2: escanearShapefiles(ruta) ->" },
                { de: "control", a: "entity", texto: "1.3: -> leerCabeceraShpYAtributosDbf()" },
                { de: "entity", a: "control", texto: "1.4: <- retornarMetadatos(conteo, bbox)" },
                { de: "control", a: "boundary", texto: "1.5: <- poblarTablaCapasDetectadas()" }
            ]
        },
        {
            codigo: "CU06",
            titulo: "CU06 - Validar Integridad y Referencia Espacial WGS 84",
            actor: "Operador Migrador",
            boundary: "UI_MainMigradorForm",
            control: "ShapefileReader",
            entity: "PrjSpatialReference",
            mensajes: [
                { de: "actor", a: "boundary", texto: "1.1: pulsarValidarProyeccion() ->" },
                { de: "boundary", a: "control", texto: "1.2: validarIntegridadPrj(ruta) ->" },
                { de: "control", a: "entity", texto: "1.3: -> parsearWKT_PRJ()" },
                { de: "entity", a: "control", texto: "1.4: <- confirmarSRID4326(WGS84)" },
                { de: "control", a: "boundary", texto: "1.5: <- habilitarBotonMigracion()" }
            ]
        },
        {
            codigo: "CU08",
            titulo: "CU08 - Ejecutar Migración Masiva Transaccional por Lotes",
            actor: "Operador Migrador",
            boundary: "UI_MainMigradorForm",
            control: "BatchProcessor",
            entity: "SqlTransaction",
            mensajes: [
                { de: "actor", a: "boundary", texto: "1.1: iniciarMigracion(modalidad) ->" },
                { de: "boundary", a: "control", texto: "1.2: ejecutarMigracionLotes(1000) ->" },
                { de: "control", a: "entity", texto: "1.3: BEGIN TRANSACTION ->" },
                { de: "control", a: "entity", texto: "1.4: INSERT Bulk con STIsValid() ->" },
                { de: "control", a: "entity", texto: "1.5: COMMIT TRANSACTION ->" },
                { de: "control", a: "boundary", texto: "1.6: <- reportarProgresoYBitacora()" }
            ]
        },
        {
            codigo: "CU11",
            titulo: "CU11 - Visualizar Mapa Base y Administrar Capas",
            actor: "Consultor",
            boundary: "UI_VisorLeafletMap",
            control: "CapasApiController",
            entity: "CatalogoCapas",
            mensajes: [
                { de: "actor", a: "boundary", texto: "1.1: conmutarCapa(nombre, visible) ->" },
                { de: "boundary", a: "control", texto: "1.2: getGeoJsonBBox(bbox, capa) ->" },
                { de: "control", a: "entity", texto: "1.3: -> consultarEntidadesBBox()" },
                { de: "entity", a: "control", texto: "1.4: <- retornarFeatureCollectionGeoJSON()" },
                { de: "control", a: "boundary", texto: "1.5: <- renderizarVectoresEnLienzo()" },
                { de: "boundary", a: "actor", texto: "1.6: <- actualizarLeyendaSimbologia()" }
            ]
        },
        {
            codigo: "CU13",
            titulo: "CU13 - Identificar Entidades por Clic / Toque",
            actor: "Consultor",
            boundary: "UI_EntityDrawer",
            control: "SpatialEngine",
            entity: "LotePredio",
            mensajes: [
                { de: "actor", a: "boundary", texto: "1.1: clicEnGeometria(lat, lon) ->" },
                { de: "boundary", a: "control", texto: "1.2: identificarPunto(lat, lon, tol) ->" },
                { de: "control", a: "entity", texto: "1.3: -> STIntersects(Point, Geometria)" },
                { de: "entity", a: "control", texto: "1.4: <- retornarFichaPredialAtributos()" },
                { de: "control", a: "boundary", texto: "1.5: <- resaltarGeometriaYCargarDrawer()" }
            ]
        },
        {
            codigo: "CU14",
            titulo: "CU14 - Búsqueda Predictiva Multicriterio",
            actor: "Consultor",
            boundary: "UI_SearchBar",
            control: "BusquedaApiController",
            entity: "IndiceTextoPredial",
            mensajes: [
                { de: "actor", a: "boundary", texto: "1.1: ingresarTextoBusqueda(q, capa) ->" },
                { de: "boundary", a: "control", texto: "1.2: buscarElementos(criterio) ->" },
                { de: "control", a: "entity", texto: "1.3: -> consultarPorCodigoTitularDireccion()" },
                { de: "entity", a: "control", texto: "1.4: <- retornarCoincidencias()" },
                { de: "control", a: "boundary", texto: "1.5: <- mostrarResultadosYZoomGeometria()" }
            ]
        },
        {
            codigo: "CU15",
            titulo: "CU15 - Medición Cartográfica (Distancia y Área)",
            actor: "Consultor",
            boundary: "UI_PanelHerramientas",
            control: "MeasurementEngine",
            entity: "CalculoGeodesico",
            mensajes: [
                { de: "actor", a: "boundary", texto: "1.1: activarReglaDistanciaOArea() ->" },
                { de: "actor", a: "boundary", texto: "1.2: clicsEnVerticesDelMapa() ->" },
                { de: "boundary", a: "control", texto: "1.3: calcularDistanciaOArea(coords) ->" },
                { de: "control", a: "entity", texto: "1.4: -> formulaVincentyHaversine()" },
                { de: "control", a: "boundary", texto: "1.5: <- mostrarTooltipValorCalculado(m, m2)" }
            ]
        },
        {
            codigo: "CU16",
            titulo: "CU16 - Geolocalización GPS y Coordenadas",
            actor: "Consultor / Operario",
            boundary: "UI_HUDCoordenadas",
            control: "LocationService",
            entity: "CoordenadaWGS84",
            mensajes: [
                { de: "actor", a: "boundary", texto: "1.1: pulsarMiUbicacion() ->" },
                { de: "boundary", a: "control", texto: "1.2: solicitarPosicionActualGPS() ->" },
                { de: "control", a: "entity", texto: "1.3: -> obtenerLatLonPrecision()" },
                { de: "control", a: "boundary", texto: "1.4: <- centrarMapaYDibujarPuntoPulsante()" },
                { de: "boundary", a: "actor", texto: "1.5: <- actualizarHUD(EPSG:4326)" }
            ]
        },
        {
            codigo: "CU17",
            titulo: "CU17 - Exportación de Información (Logs y WKT)",
            actor: "Consultor / Admin",
            boundary: "UI_ExportDialog",
            control: "ExportController",
            entity: "CsvExportStream",
            mensajes: [
                { de: "actor", a: "boundary", texto: "1.1: solicitarExportarCSV() ->" },
                { de: "boundary", a: "control", texto: "1.2: generarExportacion(tipo, filtros) ->" },
                { de: "control", a: "entity", texto: "1.3: -> generarStreamCSV_UTF8()" },
                { de: "control", a: "boundary", texto: "1.4: <- descargarArchivo('Auditoria_SIG.csv')" }
            ]
        }
    ];

    for (var i = 0; i < casos.length; i++) {
        var c = casos[i];
        Session.Output("-> Diagrama de Comunicación: " + c.codigo);

        var diag as EA.Diagram;
        diag = pkgCom.Diagrams.AddNew(c.titulo, "Communication");
        diag.Update();

        // 1. Elementos con Estereotipos de Robustez
        var elActor as EA.Element;
        elActor = pkgCom.Elements.AddNew(c.actor, "Actor");
        elActor.Update();

        var elBoundary as EA.Element;
        elBoundary = pkgCom.Elements.AddNew(c.boundary, "Boundary");
        elBoundary.Stereotype = "boundary";
        elBoundary.Update();

        var elControl as EA.Element;
        elControl = pkgCom.Elements.AddNew(c.control, "Control");
        elControl.Stereotype = "control";
        elControl.Update();

        var elEntity as EA.Element;
        elEntity = pkgCom.Elements.AddNew(c.entity, "Entity");
        elEntity.Stereotype = "entity";
        elEntity.Update();

        // Posicionar elementos (disposición en abanico / L como en la imagen de referencia)
        addObj(diag, elActor, 80, 160, 180, 260);
        addObj(diag, elBoundary, 280, 140, 400, 260);
        addObj(diag, elControl, 540, 140, 660, 260);
        addObj(diag, elEntity, 540, 340, 660, 460);

        // Crear enlaces
        conectarConMensajes(elActor, elBoundary, c.mensajes, "actor", "boundary");
        conectarConMensajes(elBoundary, elControl, c.mensajes, "boundary", "control");
        conectarConMensajes(elControl, elEntity, c.mensajes, "control", "entity");

        diag.Update();
    }

    pkgCom.Update();
    Repository.RefreshModelView(pkgCom.PackageID);
    Session.Output(">> ¡Diagramas de Comunicación completados!");
}

function addObj(diag, elem, l, t, r, b) {
    var o as EA.DiagramObject;
    o = diag.DiagramObjects.AddNew("l=" + l + ";r=" + r + ";t=" + t + ";b=" + b + ";", "");
    o.ElementID = elem.ElementID;
    o.Update();
}

function conectarConMensajes(elem1, elem2, lista, tipo1, tipo2) {
    var conn as EA.Connector;
    conn = elem1.Connectors.AddNew("", "CommunicationPath");
    conn.SupplierID = elem2.ElementID;
    conn.Update();

    for (var m = 0; m < lista.length; m++) {
        var item = lista[m];
        var match = false;
        if ((item.de == tipo1 && item.a == tipo2) || (item.de == tipo2 && item.a == tipo1)) {
            match = true;
        }
        if (match) {
            var msg as EA.Message;
            msg = conn.Messages.AddNew(item.texto, "");
            msg.SequenceNum = (m + 1);
            msg.Update();
        }
    }
}

main();

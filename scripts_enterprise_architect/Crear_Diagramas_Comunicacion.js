!INC Local Scripts.EAConstants-JScript

/**
 * ==============================================================================
 * SISTEMA: VisorDatosSIG 2026 - UAGRM FICCT
 * SCRIPT: Generador Automático de Diagramas de Comunicación (BCE / Robustness)
 * MOTOR: Sparx Systems Enterprise Architect (JScript / JavaScript)
 * AUTORES: Bernardo Chávez / Miguel Angel Mendieta
 * ==============================================================================
 * INSTRUCCIONES EN ENTERPRISE ARCHITECT:
 * 1. Abra Enterprise Architect y cargue su proyecto (.qea / .eap / .feap).
 * 2. En el Project Browser (Árbol del Proyecto), seleccione el paquete donde
 *    desea crear los diagramas (por ejemplo: "2.3.2. Diagramas de Comunicación").
 * 3. Vaya al menú: Tools -> Scripting (o Specialize -> Tools -> Scripting).
 * 4. Cree un nuevo script de tipo "JScript", pegue este código completo y haga clic en "Run".
 * 5. Enterprise Architect creará el subpaquete con los 11 diagramas de comunicación,
 *    sus elementos Boundary, Control, Entity y los enlaces con mensajes secuenciados.
 * ==============================================================================
 */

function main() {
    Repository.EnsureOutputVisible("Script");
    Session.Output(">> Iniciando generación de Diagramas de Comunicación VisorDatosSIG 2026...");

    var targetPackage as EA.Package;
    targetPackage = Repository.GetTreeSelectedPackage();

    if (targetPackage == null) {
        Session.Prompt("Por favor, seleccione un paquete en el Project Browser antes de ejecutar el script.", promptOK);
        return;
    }

    // Crear subpaquete contenedor
    var pkgCom as EA.Package;
    pkgCom = targetPackage.Packages.AddNew("2.3.2. Diagramas de Comunicación", "");
    pkgCom.Update();

    var casos = [
        {
            codigo: "CU01",
            titulo: "CU01 - Gestión de Sesión y Autenticación",
            actor: "Usuario / Administrador",
            boundary: "LoginView (Login.cshtml)",
            control: "AutenticacionController",
            entity: "Usuario (Credenciales & Hash PBKDF2)",
            mensajes: [
                { de: "actor", a: "boundary", num: "1", texto: "IngresarCredenciales(usuario, clave)" },
                { de: "boundary", a: "control", num: "2", texto: "ValidarCredenciales(dto)" },
                { de: "control", a: "entity", num: "3", texto: "BuscarPorUsername(user)" },
                { de: "entity", a: "control", num: "4", texto: "RetornarHashYSalt()" },
                { de: "control", a: "boundary", num: "5", texto: "EmitirCookieSesion(Claims)" },
                { de: "boundary", a: "actor", num: "6", texto: "RedireccionarAlVisor()" }
            ]
        },
        {
            codigo: "CU02",
            titulo: "CU02 - Gestión de Usuarios y Permisos PBAC",
            actor: "Administrador",
            boundary: "UsuariosAdminView (Usuarios.cshtml)",
            control: "AdminController / UsuarioService",
            entity: "Usuario & UsuarioPermisos (PBAC)",
            mensajes: [
                { de: "actor", a: "boundary", num: "1", texto: "SolicitarListaYPermisos()" },
                { de: "boundary", a: "control", num: "2", texto: "ObtenerUsuariosYPermisos()" },
                { de: "control", a: "entity", num: "3", texto: "ConsultarCatalogoPermisos()" },
                { de: "entity", a: "control", num: "4", texto: "RetornarMatrizPermisos()" },
                { de: "boundary", a: "control", num: "5", texto: "GuardarPermisosUsuario(id, permisos)" },
                { de: "control", a: "entity", num: "6", texto: "ActualizarPermisosPBAC()" },
                { de: "control", a: "boundary", num: "7", texto: "ConfirmarActualizacion()" }
            ]
        },
        {
            codigo: "CU05",
            titulo: "CU05 - Seleccionar e Inspeccionar Archivos Shapefile",
            actor: "Operador Migrador",
            boundary: "MainMigradorForm (WinForms)",
            control: "MigradorService",
            entity: "ShapefileHeader & DBFTable",
            mensajes: [
                { de: "actor", a: "boundary", num: "1", texto: "SeleccionarRutaDirectorio()" },
                { de: "boundary", a: "control", num: "2", texto: "EscanearArchivosShapefiles(ruta)" },
                { de: "control", a: "entity", num: "3", texto: "LeerCabeceraShpYAtributosDbf()" },
                { de: "entity", a: "control", num: "4", texto: "RetornarMetadatosYConteo()" },
                { de: "control", a: "boundary", num: "5", texto: "PoblarTablaCapasDetectadas()" }
            ]
        },
        {
            codigo: "CU06",
            titulo: "CU06 - Validar Integridad y Referencia Espacial WGS 84",
            actor: "Operador Migrador",
            boundary: "MainMigradorForm (Vista Previa)",
            control: "MigradorService / ShapefileReader",
            entity: "PrjSpatialReference & GeometryCRS",
            mensajes: [
                { de: "actor", a: "boundary", num: "1", texto: "PulsarValidarProyeccion()" },
                { de: "boundary", a: "control", num: "2", texto: "ValidarIntegridadPrj(shpPath)" },
                { de: "control", a: "entity", num: "3", texto: "VerificarEPSG4326(WKT_PRJ)" },
                { de: "entity", a: "control", num: "4", texto: "ConfirmarSRID4326Valido()" },
                { de: "control", a: "boundary", num: "5", texto: "HabilitarBotonMigracion()" }
            ]
        },
        {
            codigo: "CU08",
            titulo: "CU08 - Ejecutar Migración Masiva Transaccional por Lotes",
            actor: "Operador Migrador",
            boundary: "MainMigradorForm (Barra Progreso)",
            control: "MigradorService / BatchProcessor",
            entity: "SqlTransaction & CapaGeometria",
            mensajes: [
                { de: "actor", a: "boundary", num: "1", texto: "IniciarMigracion(modalidad)" },
                { de: "boundary", a: "control", num: "2", texto: "EjecutarMigracionPorLotes(1000)" },
                { de: "control", a: "entity", num: "3", texto: "IniciarTransaccionSQL()" },
                { de: "control", a: "entity", num: "4", texto: "InsertarLotesConSTIsValid()" },
                { de: "entity", a: "control", num: "5", texto: "CommitTransaccion()" },
                { de: "control", a: "boundary", num: "6", texto: "ReportarProgresoYBitacora(resumen)" }
            ]
        },
        {
            codigo: "CU11",
            titulo: "CU11 - Visualizar Mapa Base y Administrar Capas Temáticas",
            actor: "Consultor / Administrador",
            boundary: "VisorMapView (Leaflet Canvas)",
            control: "CapasApiController / CapaService",
            entity: "CatalogoCapas & GeoJsonBBox",
            mensajes: [
                { de: "actor", a: "boundary", num: "1", texto: "ConmutarCapa(nombre, visible)" },
                { de: "boundary", a: "control", num: "2", texto: "GetGeoJsonPorBoundingBox(bbox, capa)" },
                { de: "control", a: "entity", num: "3", texto: "ConsultarEntidadesIntersectanBBox()" },
                { de: "entity", a: "control", num: "4", texto: "RetornarFeatureCollectionGeoJson()" },
                { de: "control", a: "boundary", num: "5", texto: "RenderizarVectoresEnLienzo(estilos)" },
                { de: "boundary", a: "actor", num: "6", texto: "ActualizarLeyendaSimbologia()" }
            ]
        },
        {
            codigo: "CU13",
            titulo: "CU13 - Identificar Entidades Geográficas por Clic",
            actor: "Consultor",
            boundary: "VisorMapView & EntityDrawer",
            control: "CapasApiController / SpatialEngine",
            entity: "Lote / Manzana / MedidorAgua",
            mensajes: [
                { de: "actor", a: "boundary", num: "1", texto: "ClicEnGeometria(lat, lon)" },
                { de: "boundary", a: "control", num: "2", texto: "IdentificarPunto(lat, lon, tolerancia)" },
                { de: "control", a: "entity", num: "3", texto: "STIntersects(Point, Geometria)" },
                { de: "entity", a: "control", num: "4", texto: "RetornarFichaPredialAtributos()" },
                { de: "control", a: "boundary", num: "5", texto: "ResaltarGeometriaYCargarDrawer()" }
            ]
        },
        {
            codigo: "CU14",
            titulo: "CU14 - Búsqueda Predictiva Multicriterio y Filtro",
            actor: "Consultor",
            boundary: "VisorMapView (Barra Buscador)",
            control: "BusquedaApiController / BusquedaService",
            entity: "Lotes & Vias (Indices Texto)",
            mensajes: [
                { de: "actor", a: "boundary", num: "1", texto: "IngresarTextoBusqueda(termino, capa)" },
                { de: "boundary", a: "control", num: "2", texto: "BuscarElementos(criterio)" },
                { de: "control", a: "entity", num: "3", texto: "ConsultarPorCodigoTitularDireccion()" },
                { de: "entity", a: "control", num: "4", texto: "RetornarListaCoincidencias()" },
                { de: "control", a: "boundary", num: "5", texto: "MostrarResultadosYZoomGeometria()" }
            ]
        },
        {
            codigo: "CU15",
            titulo: "CU15 - Herramientas de Medición Cartográfica",
            actor: "Consultor",
            boundary: "VisorMapView (Panel Herramientas)",
            control: "MeasurementToolController (JS Client)",
            entity: "GeodesicMeasurement (m, km, m2, ha)",
            mensajes: [
                { de: "actor", a: "boundary", num: "1", texto: "ActivarReglaDistanciaOArea()" },
                { de: "boundary", a: "control", num: "2", texto: "InicializarTrazadoGeodesico()" },
                { de: "actor", a: "boundary", num: "3", texto: "ClicsEnVerticesDelMapa()" },
                { de: "boundary", a: "control", num: "4", texto: "CalcularDistanciaOArea(coordenadas)" },
                { de: "control", a: "entity", num: "5", texto: "FormulaVincentyHaversine()" },
                { de: "control", a: "boundary", num: "6", texto: "MostrarTooltipValorCalculado()" }
            ]
        },
        {
            codigo: "CU16",
            titulo: "CU16 - Geolocalización GPS y Navegación Coordenadas",
            actor: "Consultor / Operario",
            boundary: "VisorMapView & HUD Coordenadas",
            control: "LocationService (Navigator GPS API)",
            entity: "CoordenadasWgs84 & AccuracyCircle",
            mensajes: [
                { de: "actor", a: "boundary", num: "1", texto: "PulsarMiUbicacionOGoToCoords()" },
                { de: "boundary", a: "control", num: "2", texto: "SolicitarPosicionActualGPS()" },
                { de: "control", a: "entity", num: "3", texto: "ObtenerLatLonMargenPrecision()" },
                { de: "entity", a: "control", num: "4", texto: "RetornarPosicion(lat, lon, acc)" },
                { de: "control", a: "boundary", num: "5", texto: "CentrarMapaYDibujarMarcadorPulsante()" },
                { de: "boundary", a: "actor", num: "6", texto: "ActualizarHUDCoordenadas(EPSG4326)" }
            ]
        },
        {
            codigo: "CU17",
            titulo: "CU17 - Exportación de Información (Logs y WKT)",
            actor: "Consultor / Administrador",
            boundary: "VisorMapView & AuditoriaView",
            control: "ExportController / AuditoriaService",
            entity: "CsvExportStream & WktGeometryString",
            mensajes: [
                { de: "actor", a: "boundary", num: "1", texto: "SolicitarExportarCSVOCopiarWKT()" },
                { de: "boundary", a: "control", num: "2", texto: "GenerarExportacion(tipo, id)" },
                { de: "control", a: "entity", num: "3", texto: "ObtenerWKTOGenerarStreamCSV()" },
                { de: "entity", a: "control", num: "4", texto: "RetornarBufferDatosUTF8()" },
                { de: "control", a: "boundary", num: "5", texto: "DescargarArchivoOCopiarClipboard()" }
            ]
        }
    ];

    for (var i = 0; i < casos.length; i++) {
        var c = casos[i];
        Session.Output("-> Creando diagrama de comunicación para: " + c.codigo);

        // 1. Crear el diagrama de tipo Communication
        var diag as EA.Diagram;
        diag = pkgCom.Diagrams.AddNew(c.titulo, "Communication");
        diag.Update();

        // 2. Crear elementos BCE
        var elActor as EA.Element;
        elActor = pkgCom.Elements.AddNew(c.actor, "Actor");
        elActor.Update();

        var elBoundary as EA.Element;
        elBoundary = pkgCom.Elements.AddNew(c.boundary, "Boundary");
        elBoundary.Update();

        var elControl as EA.Element;
        elControl = pkgCom.Elements.AddNew(c.control, "Control");
        elControl.Update();

        var elEntity as EA.Element;
        elEntity = pkgCom.Elements.AddNew(c.entity, "Entity");
        elEntity.Update();

        // 3. Posicionar en el diagrama (disposición horizontal armónica)
        addDiagramObject(diag, elActor, 80, 200, 180, 290);
        addDiagramObject(diag, elBoundary, 250, 170, 420, 310);
        addDiagramObject(diag, elControl, 490, 170, 670, 310);
        addDiagramObject(diag, elEntity, 740, 170, 930, 310);

        // 4. Crear conectores y asociar mensajes de comunicación
        crearEnlaceYMensajes(elActor, elBoundary, c.mensajes);
        crearEnlaceYMensajes(elBoundary, elControl, c.mensajes);
        crearEnlaceYMensajes(elControl, elEntity, c.mensajes);

        diag.Update();
    }

    pkgCom.Update();
    Repository.RefreshModelView(pkgCom.PackageID);
    Session.Output(">> ¡Generación de Diagramas de Comunicación finalizada con éxito!");
    Session.Prompt("Se crearon exitosamente los 11 Diagramas de Comunicación en el paquete seleccionado.", promptOK);
}

function addDiagramObject(diagram, element, left, top, right, bottom) {
    var diagObj as EA.DiagramObject;
    diagObj = diagram.DiagramObjects.AddNew("l=" + left + ";r=" + right + ";t=" + top + ";b=" + bottom + ";", "");
    diagObj.ElementID = element.ElementID;
    diagObj.Update();
}

function crearEnlaceYMensajes(elem1, elem2, listaMensajes) {
    var conn as EA.Connector;
    conn = elem1.Connectors.AddNew("", "CommunicationPath");
    conn.SupplierID = elem2.ElementID;
    conn.Update();

    for (var j = 0; j < listaMensajes.length; j++) {
        var m = listaMensajes[j];
        var aplica = false;
        if (m.de == "actor" && m.a == "boundary" && elem1.Type == "Actor" && elem2.Type == "Boundary") aplica = true;
        if (m.de == "boundary" && m.a == "actor" && elem1.Type == "Actor" && elem2.Type == "Boundary") aplica = true;
        if (m.de == "boundary" && m.a == "control" && elem1.Type == "Boundary" && elem2.Type == "Control") aplica = true;
        if (m.de == "control" && m.a == "boundary" && elem1.Type == "Boundary" && elem2.Type == "Control") aplica = true;
        if (m.de == "control" && m.a == "entity" && elem1.Type == "Control" && elem2.Type == "Entity") aplica = true;
        if (m.de == "entity" && m.a == "control" && elem1.Type == "Control" && elem2.Type == "Entity") aplica = true;

        if (aplica) {
            var msg as EA.Message;
            msg = conn.Messages.AddNew(m.num + ": " + m.texto, "");
            msg.SequenceNum = parseInt(m.num, 10);
            msg.Update();
        }
    }
}

main();

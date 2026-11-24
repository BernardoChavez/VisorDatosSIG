!INC Local Scripts.EAConstants-JScript

/**
 * ==============================================================================
 * SISTEMA: VisorDatosSIG 2026 - UAGRM FICCT
 * SCRIPT: Generador de Diagramas de Secuencia (Lifelines con Iconos BCE y alt)
 * ==============================================================================
 */

function main() {
    Repository.EnsureOutputVisible("Script");
    Session.Output(">> Generando Diagramas de Secuencia (Con Iconos de Robustez y alt)...");

    var targetPackage as EA.Package;
    targetPackage = Repository.GetTreeSelectedPackage();

    if (targetPackage == null) {
        Session.Prompt("Seleccione un paquete en el Project Browser.", promptOK);
        return;
    }

    var pkgSec as EA.Package;
    pkgSec = targetPackage.Packages.AddNew("2.4.3. Diagramas de Secuencia", "");
    pkgSec.Update();

    var diagramas = [
        {
            codigo: "CU01",
            titulo: "CU01 - Gestión de Sesión y Autenticación (Login)",
            actorNombre: "u: Usuario",
            lifelines: [
                { nombre: "view: LoginView", tipo: "Boundary", pos: 220 },
                { nombre: "ctrl: AutenticacionController", tipo: "Control", pos: 430 },
                { nombre: "srv: UsuarioService", tipo: "Control", pos: 640 },
                { nombre: "ent: Usuario", tipo: "Entity", pos: 850 }
            ],
            fragmentos: [
                { nombre: "alt Credenciales Válidas", cond: "[Credenciales Correctas]", l: 60, r: 960, t: 80, b: 320 },
                { nombre: "alt Credenciales Inválidas", cond: "[Fallo Contraseña o Bloqueo]", l: 60, r: 960, t: 340, b: 500 }
            ],
            mensajes: [
                { de: -1, a: 0, texto: "1: ingresarCredenciales(user, pass)" },
                { de: 0, a: 1, texto: "2: POST /api/autenticacion/login" },
                { de: 1, a: 2, texto: "3: autenticarAsync(user, pass)" },
                { de: 2, a: 3, texto: "4: buscarPorUsername(user)" },
                { de: 3, a: 2, texto: "5: retorno(Hash, Salt, Rol)" },
                { de: 2, a: 2, texto: "6: verificarPBKDF2(pass, Salt)" },
                { de: 2, a: 1, texto: "7: retorno TokenSesion / Claims" },
                { de: 1, a: 0, texto: "8: emitirCookie(HttpOnly)" },
                { de: 0, a: -1, texto: "9: redireccionar a /Home/Index" },
                // Fragmento invalido
                { de: 2, a: 1, texto: "10: notificarError('Clave incorrecta')" },
                { de: 1, a: 0, texto: "11: mostrarAlertaError()" }
            ]
        },
        {
            codigo: "CU06",
            titulo: "CU06 - Validación de Integridad y WGS 84 (SRID 4326)",
            actorNombre: "op: OperadorMigrador",
            lifelines: [
                { nombre: "form: MainMigradorForm", tipo: "Boundary", pos: 240 },
                { nombre: "srv: MigradorService", tipo: "Control", pos: 480 },
                { nombre: "reader: ShapefileReader", tipo: "Control", pos: 700 },
                { nombre: "prj: PrjSpatialReference", tipo: "Entity", pos: 920 }
            ],
            fragmentos: [
                { nombre: "alt Validación CRS", cond: "[PRJ contiene EPSG:4326]", l: 60, r: 1020, t: 80, b: 350 }
            ],
            mensajes: [
                { de: -1, a: 0, texto: "1: pulsar 'Validar Proyección y CRS'" },
                { de: 0, a: 1, texto: "2: validarArchivoShapefileAsync(ruta)" },
                { de: 1, a: 2, texto: "3: inspeccionarPrj(rutaPrj)" },
                { de: 2, a: 3, texto: "4: parsearWktSpatialReference()" },
                { de: 3, a: 2, texto: "5: retorno (EsWGS84=true, SRID=4326)" },
                { de: 2, a: 1, texto: "6: retorno ValidacionDto(Aprobado)" },
                { de: 1, a: 0, texto: "7: habilitar botón 'Iniciar Migración'" },
                { de: 0, a: -1, texto: "8: mostrar badge verde 'WGS 84 Válido'" }
            ]
        },
        {
            codigo: "CU08",
            titulo: "CU08 - Migración Masiva Transaccional por Lotes",
            actorNombre: "op: OperadorMigrador",
            lifelines: [
                { nombre: "form: MainMigradorForm", tipo: "Boundary", pos: 220 },
                { nombre: "srv: MigradorService", tipo: "Control", pos: 440 },
                { nombre: "batch: BatchProcessor", tipo: "Control", pos: 660 },
                { nombre: "tx: SqlTransaction", tipo: "Entity", pos: 880 }
            ],
            fragmentos: [
                { nombre: "loop Lotes de 1000", cond: "[Por cada 1000 entidades]", l: 60, r: 980, t: 150, b: 360 }
            ],
            mensajes: [
                { de: -1, a: 0, texto: "1: clic 'Iniciar Migración Masiva'" },
                { de: 0, a: 1, texto: "2: migrarCapaAsync(opciones)" },
                { de: 1, a: 3, texto: "3: BEGIN TRANSACTION" },
                { de: 1, a: 2, texto: "4: insertarLote(batch, SRID=4326)" },
                { de: 2, a: 3, texto: "5: BulkInsert con STIsValid() = 1" },
                { de: 1, a: 0, texto: "6: reportarProgreso(porcentaje, stats)" },
                { de: 1, a: 3, texto: "7: COMMIT TRANSACTION" },
                { de: 1, a: 0, texto: "8: retorno ResumenMigracionDto" },
                { de: 0, a: -1, texto: "9: desplegar estadísticas finales" }
            ]
        },
        {
            codigo: "CU11",
            titulo: "CU11 - Carga de Visor y GeoJSON BBox",
            actorNombre: "c: Consultor",
            lifelines: [
                { nombre: "map: LeafletMap", tipo: "Boundary", pos: 230 },
                { nombre: "api: CapasApiController", tipo: "Control", pos: 460 },
                { nombre: "srv: CapaService", tipo: "Control", pos: 680 },
                { nombre: "ent: LotePredio", tipo: "Entity", pos: 900 }
            ],
            fragmentos: [
                { nombre: "alt Consulta Territorial", cond: "[Entidades intersectan BoundingBox]", l: 60, r: 1000, t: 80, b: 380 }
            ],
            mensajes: [
                { de: -1, a: 0, texto: "1: pan / zoom en el mapa" },
                { de: 0, a: 1, texto: "2: GET /api/capas/geojson?capa=Lotes&bbox=..." },
                { de: 1, a: 2, texto: "3: obtenerGeoJsonBBoxAsync(capa, bbox)" },
                { de: 2, a: 3, texto: "4: SELECT WHERE Geometria.Filter(bbox) = 1" },
                { de: 3, a: 2, texto: "5: retorno SqlDataReader (WKT)" },
                { de: 2, a: 1, texto: "6: serializar FeatureCollection (Gzip)" },
                { de: 1, a: 0, texto: "7: 200 OK (GeoJSON RFC 7946)" },
                { de: 0, a: 0, texto: "8: L.geoJSON().addTo(map)" },
                { de: 0, a: -1, texto: "9: renderizado vectorial con estilos" }
            ]
        },
        {
            codigo: "CU13",
            titulo: "CU13 - Identificación por Clic / Toque",
            actorNombre: "c: Consultor",
            lifelines: [
                { nombre: "map: LeafletMap", tipo: "Boundary", pos: 220 },
                { nombre: "drawer: EntityDrawer", tipo: "Boundary", pos: 430 },
                { nombre: "api: CapasApiController", tipo: "Control", pos: 650 },
                { nombre: "ent: LotePredio", tipo: "Entity", pos: 870 }
            ],
            fragmentos: [
                { nombre: "alt Predio Encontrado", cond: "[Intersects(Point) == 1]", l: 60, r: 970, t: 80, b: 350 }
            ],
            mensajes: [
                { de: -1, a: 0, texto: "1: clic en geometría (lat, lon)" },
                { de: 0, a: 2, texto: "2: GET /api/capas/identificar?lat=Y&lon=X" },
                { de: 2, a: 3, texto: "3: STIntersects(Point, Geometria)" },
                { de: 3, a: 2, texto: "4: retorno Atributos Catastrales" },
                { de: 2, a: 0, texto: "5: 200 OK (FichaPredialDto + WKT)" },
                { de: 0, a: 0, texto: "6: resaltar polígono (stroke cian)" },
                { de: 0, a: 1, texto: "7: abrir panel y cargar datos" },
                { de: 1, a: -1, texto: "8: visualización de titular, área y código" }
            ]
        },
        {
            codigo: "CU14",
            titulo: "CU14 - Búsqueda Predictiva Multicriterio",
            actorNombre: "c: Consultor",
            lifelines: [
                { nombre: "sb: SearchBar", tipo: "Boundary", pos: 230 },
                { nombre: "api: BusquedaApiController", tipo: "Control", pos: 460 },
                { nombre: "srv: BusquedaService", tipo: "Control", pos: 680 },
                { nombre: "map: LeafletMap", tipo: "Boundary", pos: 900 }
            ],
            fragmentos: [
                { nombre: "alt Resultados", cond: "[Coincidencias > 0]", l: 60, r: 1000, t: 80, b: 380 }
            ],
            mensajes: [
                { de: -1, a: 0, texto: "1: escribir término (ej. 'M-12' o titular)" },
                { de: 0, a: 1, texto: "2: GET /api/busqueda/rapida?q=termino" },
                { de: 1, a: 2, texto: "3: buscarAsync(query, capa)" },
                { de: 2, a: 1, texto: "4: retorno List<ResultadoBusquedaDto>" },
                { de: 1, a: 0, texto: "5: 200 OK desplegar lista en sidebar" },
                { de: -1, a: 0, texto: "6: seleccionar predio de la lista" },
                { de: 0, a: 3, texto: "7: fitBounds(bbox) + highlight()" },
                { de: 3, a: -1, texto: "8: zoom suave y enfoque centrado" }
            ]
        },
        {
            codigo: "CU15",
            titulo: "CU15 - Medición Cartográfica (Distancia y Área)",
            actorNombre: "c: Consultor",
            lifelines: [
                { nombre: "ph: PanelHerramientas", tipo: "Boundary", pos: 230 },
                { nombre: "engine: MeasurementEngine", tipo: "Control", pos: 470 },
                { nombre: "map: LeafletMap", tipo: "Boundary", pos: 710 }
            ],
            fragmentos: [
                { nombre: "loop Clics Vértices", cond: "[Hasta doble clic]", l: 60, r: 850, t: 120, b: 320 }
            ],
            mensajes: [
                { de: -1, a: 0, texto: "1: clic 'Medir Distancia' o 'Medir Área'" },
                { de: 0, a: 1, texto: "2: activarModoMedicion(tipo)" },
                { de: 1, a: 2, texto: "3: cambiar cursor a Crosshair" },
                { de: -1, a: 2, texto: "4: clics sucesivos en mapa" },
                { de: 2, a: 1, texto: "5: map.click(latlng)" },
                { de: 1, a: 1, texto: "6: calcularFormulaGeodesicaHaversine()" },
                { de: 1, a: 2, texto: "7: actualizarTooltip(m, km, m2, ha)" },
                { de: -1, a: 2, texto: "8: doble clic finaliza trazado" },
                { de: 1, a: -1, texto: "9: fijar resultado métrico en pantalla" }
            ]
        },
        {
            codigo: "CU16",
            titulo: "CU16 - Geolocalización GPS y Coordenadas",
            actorNombre: "u: Usuario / Operario",
            lifelines: [
                { nombre: "hud: ControlesHUD", tipo: "Boundary", pos: 230 },
                { nombre: "gps: GeolocationService", tipo: "Control", pos: 480 },
                { nombre: "map: LeafletMap", tipo: "Boundary", pos: 720 }
            ],
            fragmentos: [
                { nombre: "alt Permiso Concedido", cond: "[Permiso GPS Otorgado]", l: 60, r: 850, t: 80, b: 320 }
            ],
            mensajes: [
                { de: -1, a: 0, texto: "1: pulsar 'Detectar Mi Ubicación'" },
                { de: 0, a: 1, texto: "2: getCurrentPosition(highAccuracy)" },
                { de: 1, a: 0, texto: "3: retorno Position(lat, lon, accuracy)" },
                { de: 0, a: 2, texto: "4: setView([lat, lon], 17)" },
                { de: 0, a: 2, texto: "5: agregar L.circleMarker pulsante" },
                { de: 0, a: -1, texto: "6: actualizar HUD: 'EPSG:4326'" }
            ]
        },
        {
            codigo: "CU17",
            titulo: "CU17 - Exportación de Información (Logs y WKT)",
            actorNombre: "u: Consultor / Admin",
            lifelines: [
                { nombre: "ui: AuditoriaView", tipo: "Boundary", pos: 230 },
                { nombre: "ctrl: ExportController", tipo: "Control", pos: 480 },
                { nombre: "srv: AuditoriaService", tipo: "Control", pos: 720 }
            ],
            fragmentos: [
                { nombre: "alt Formato Solicitado", cond: "[Descarga CSV]", l: 60, r: 850, t: 80, b: 300 }
            ],
            mensajes: [
                { de: -1, a: 0, texto: "1: clic 'Exportar CSV' o 'Copiar WKT'" },
                { de: 0, a: 1, texto: "2: generarExportacion(filtros)" },
                { de: 1, a: 2, texto: "3: exportarRegistrosCsvAsync()" },
                { de: 2, a: 1, texto: "4: retorno FileContentResult (UTF8)" },
                { de: 1, a: 0, texto: "5: descarga 'Auditoria_SIG.csv'" },
                { de: 0, a: -1, texto: "6: confirmación de archivo descargado" }
            ]
        }
    ];

    for (var i = 0; i < diagramas.length; i++) {
        var d = diagramas[i];
        Session.Output("-> Diagrama de Secuencia: " + d.codigo);

        var diag as EA.Diagram;
        diag = pkgSec.Diagrams.AddNew(d.titulo, "Sequence");
        diag.Update();

        // 1. Crear el Actor
        var elActor as EA.Element;
        elActor = pkgSec.Elements.AddNew(d.actorNombre, "Actor");
        elActor.Update();
        addObj(diag, elActor, 60, 50, 160, 550);

        var lifelinesList = [];
        lifelinesList.push(elActor);

        // 2. Crear las Lifelines con Clasificador de Robustez (para que dibuje el icono BCE arriba)
        for (var l = 0; l < d.lifelines.length; l++) {
            var item = d.lifelines[l];

            // Crear el clasificador de robustez
            var clasificador as EA.Element;
            clasificador = pkgSec.Elements.AddNew(item.nombre.split(":")[1].trim(), item.tipo);
            clasificador.Stereotype = item.tipo.toLowerCase();
            clasificador.Update();

            // Crear la Lifeline
            var lf as EA.Element;
            lf = pkgSec.Elements.AddNew(item.nombre, "Sequence");
            lf.ClassifierID = clasificador.ElementID; // <-- Asocia el clasificador para mostrar icono BCE
            lf.Update();

            addObj(diag, lf, item.pos, 50, item.pos + 120, 550);
            lifelinesList.push(lf);
        }

        // 3. Crear fragmentos combinados (alt / loop)
        for (var f = 0; f < d.fragmentos.length; f++) {
            var fr = d.fragmentos[f];
            var fragElem as EA.Element;
            fragElem = pkgSec.Elements.AddNew(fr.nombre, "InteractionFragment");
            fragElem.Subtype = 0; // CombinedFragment
            fragElem.Stereotype = fr.nombre.startsWith("loop") ? "loop" : "alt";
            fragElem.Update();

            addObj(diag, fragElem, fr.l, fr.t, fr.r, fr.b);
        }

        // 4. Crear los mensajes
        for (var m = 0; m < d.mensajes.length; m++) {
            var msgData = d.mensajes[m];
            var idxDe = msgData.de + 1; // -1 corresponde a Actor (idx 0)
            var idxA = msgData.a + 1;

            var src = lifelinesList[idxDe];
            var tgt = lifelinesList[idxA];

            var conn as EA.Connector;
            conn = src.Connectors.AddNew(msgData.texto, "Sequence");
            conn.SupplierID = tgt.ElementID;
            conn.SequenceNum = (m + 1);
            conn.Update();
        }

        diag.Update();
    }

    pkgSec.Update();
    Repository.RefreshModelView(pkgSec.PackageID);
    Session.Output(">> ¡Diagramas de Secuencia completados!");
}

function addObj(diag, elem, l, t, r, b) {
    var o as EA.DiagramObject;
    o = diag.DiagramObjects.AddNew("l=" + l + ";r=" + r + ";t=" + t + ";b=" + b + ";", "");
    o.ElementID = elem.ElementID;
    o.Update();
}

main();

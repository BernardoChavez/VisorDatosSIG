!INC Local Scripts.EAConstants-JScript

/**
 * ==============================================================================
 * SISTEMA: VisorDatosSIG 2026 - UAGRM FICCT
 * SCRIPT: Generador Automático de Diagramas de Secuencia UML
 * MOTOR: Sparx Systems Enterprise Architect (JScript / JavaScript)
 * AUTORES: Bernardo Chávez / Miguel Angel Mendieta
 * ==============================================================================
 * INSTRUCCIONES:
 * 1. Abra Enterprise Architect y su proyecto.
 * 2. Seleccione en el Project Browser el paquete donde desea alojar los diagramas
 *    (por ejemplo: "2.4.3. Diagramas de Secuencia").
 * 3. En Tools -> Scripting, cree un script JScript, pegue este contenido y pulse "Run".
 * 4. EA generará automáticamente el subpaquete con los 9 diagramas de secuencia,
 *    sus Lifelines (Actor, Vista, Controlador, Servicio, Repositorio, Base de Datos)
 *    y todos los mensajes secuenciales sincrónicos y de retorno.
 * ==============================================================================
 */

function main() {
    Repository.EnsureOutputVisible("Script");
    Session.Output(">> Iniciando generación de Diagramas de Secuencia VisorDatosSIG 2026...");

    var targetPackage as EA.Package;
    targetPackage = Repository.GetTreeSelectedPackage();

    if (targetPackage == null) {
        Session.Prompt("Por favor, seleccione un paquete en el Project Browser antes de ejecutar.", promptOK);
        return;
    }

    var pkgSec as EA.Package;
    pkgSec = targetPackage.Packages.AddNew("2.4.3. Diagramas de Secuencia", "");
    pkgSec.Update();

    var diagramas = [
        {
            codigo: "CU01",
            titulo: "CU01 - Gestión de Sesión y Autenticación (Login)",
            lifelines: [
                { nombre: "u: Usuario", tipo: "Actor", pos: 80 },
                { nombre: "view: LoginView", tipo: "Sequence", pos: 240 },
                { nombre: "ctrl: AutenticacionController", tipo: "Sequence", pos: 420 },
                { nombre: "srv: UsuarioService", tipo: "Sequence", pos: 600 },
                { nombre: "repo: UsuarioRepository", tipo: "Sequence", pos: 780 },
                { nombre: "db: SQL Server 2022", tipo: "Sequence", pos: 960 }
            ],
            mensajes: [
                { de: 0, a: 1, texto: "1: IngresarCredenciales(user, pass)" },
                { de: 1, a: 2, texto: "2: PostLogin(loginDto)" },
                { de: 2, a: 3, texto: "3: AutenticarAsync(user, pass)" },
                { de: 3, a: 4, texto: "4: ObtenerPorUsernameAsync(user)" },
                { de: 4, a: 5, texto: "5: SELECT * FROM Usuarios WHERE Username=@u" },
                { de: 5, a: 4, texto: "6: SqlDataReader (Hash, Salt, Rol)" },
                { de: 4, a: 3, texto: "7: UsuarioEntity" },
                { de: 3, a: 3, texto: "8: VerificarPBKDF2(pass, hash, salt)" },
                { de: 3, a: 2, texto: "9: TokenSesion / ClaimsPrincipal" },
                { de: 2, a: 1, texto: "10: SignInAsync (HttpOnly Cookie)" },
                { de: 1, a: 0, texto: "11: Redireccionar a /Home/Index" }
            ]
        },
        {
            codigo: "CU06",
            titulo: "CU06 - Validación de Integridad y WGS 84 (SRID 4326)",
            lifelines: [
                { nombre: "op: OperadorMigrador", tipo: "Actor", pos: 80 },
                { nombre: "form: MainMigradorForm", tipo: "Sequence", pos: 240 },
                { nombre: "srv: MigradorService", tipo: "Sequence", pos: 440 },
                { nombre: "reader: ShapefileReader", tipo: "Sequence", pos: 640 },
                { nombre: "fs: SistemaArchivos (.prj/.shp)", tipo: "Sequence", pos: 860 }
            ],
            mensajes: [
                { de: 0, a: 1, texto: "1: Clic 'Validar Proyección y CRS'" },
                { de: 1, a: 2, texto: "2: ValidarArchivoShapefileAsync(rutaShp)" },
                { de: 2, a: 3, texto: "3: InspeccionarPrj(rutaPrj)" },
                { de: 3, a: 4, texto: "4: File.ReadAllText(rutaPrj)" },
                { de: 4, a: 3, texto: "5: Contenido WKT Proyección" },
                { de: 3, a: 3, texto: "6: ParsearWktSpatialReference()" },
                { de: 3, a: 2, texto: "7: ResultadoValidacion (EsWGS84, SRID=4326)" },
                { de: 2, a: 1, texto: "8: ValidacionShapefileDto (Estado=Aprobado)" },
                { de: 1, a: 0, texto: "9: Mostrar indicador verde 'WGS 84 Válido'" }
            ]
        },
        {
            codigo: "CU08",
            titulo: "CU08 - Migración Masiva Transaccional por Lotes",
            lifelines: [
                { nombre: "op: OperadorMigrador", tipo: "Actor", pos: 80 },
                { nombre: "form: MainMigradorForm", tipo: "Sequence", pos: 240 },
                { nombre: "srv: MigradorService", tipo: "Sequence", pos: 430 },
                { nombre: "repo: MigracionRepository", tipo: "Sequence", pos: 630 },
                { nombre: "audit: AuditoriaService", tipo: "Sequence", pos: 810 },
                { nombre: "db: SQL Server 2022 Spatial", tipo: "Sequence", pos: 990 }
            ],
            mensajes: [
                { de: 0, a: 1, texto: "1: Clic 'Iniciar Migración Masiva'" },
                { de: 1, a: 2, texto: "2: MigrarCapaAsync(opciones, progreso)" },
                { de: 2, a: 3, texto: "3: IniciarTransaccionBatch(1000)" },
                { de: 3, a: 5, texto: "4: BEGIN TRANSACTION" },
                { de: 2, a: 3, texto: "5: InsertarLoteGeometrias(batch)" },
                { de: 3, a: 5, texto: "6: BulkInsert / STGeomFromText(wkt, 4326)" },
                { de: 2, a: 1, texto: "7: IProgress.Report(porcentaje, stats)" },
                { de: 3, a: 5, texto: "8: COMMIT TRANSACTION" },
                { de: 2, a: 4, texto: "9: RegistrarMigracionAsync(resumenDto)" },
                { de: 4, a: 5, texto: "10: INSERT INTO BitacoraEventos" },
                { de: 2, a: 1, texto: "11: ResumenMigracionDto (Exitoso)" },
                { de: 1, a: 0, texto: "12: Desplegar diálogo resumen y estadísticas" }
            ]
        },
        {
            codigo: "CU11",
            titulo: "CU11 - Carga de Visor y Consulta GeoJSON Bounding Box",
            lifelines: [
                { nombre: "c: Consultor", tipo: "Actor", pos: 80 },
                { nombre: "map: Leaflet.js / SigMap", tipo: "Sequence", pos: 250 },
                { nombre: "api: CapasApiController", tipo: "Sequence", pos: 450 },
                { nombre: "srv: CapaService", tipo: "Sequence", pos: 640 },
                { nombre: "repo: CapaRepository", tipo: "Sequence", pos: 820 },
                { nombre: "db: SQL Server Spatial", tipo: "Sequence", pos: 1000 }
            ],
            mensajes: [
                { de: 0, a: 1, texto: "1: Pan / Zoom en el Lienzo" },
                { de: 1, a: 2, texto: "2: GET /api/capas/geojson?capa=Lotes&bbox=minX,minY,maxX,maxY" },
                { de: 2, a: 3, texto: "3: ObtenerGeoJsonBBoxAsync(capa, bbox)" },
                { de: 3, a: 4, texto: "4: ConsultarGeometriasBBox(capa, bboxPoly)" },
                { de: 4, a: 5, texto: "5: WHERE Geometria.Filter(@bboxGeom) = 1" },
                { de: 5, a: 4, texto: "6: SqlDataReader (WKT / GeoJson)" },
                { de: 4, a: 3, texto: "7: Lista Entidades Espaciales" },
                { de: 3, a: 2, texto: "8: FeatureCollection GeoJSON (RFC 7946)" },
                { de: 2, a: 1, texto: "9: 200 OK + GeoJSON comprimido (Gzip)" },
                { de: 1, a: 1, texto: "10: L.geoJSON().addTo(map)" },
                { de: 1, a: 0, texto: "11: Renderizado dinámico de polígonos y estilos" }
            ]
        },
        {
            codigo: "CU13",
            titulo: "CU13 - Identificación por Clic / Toque y Consulta de Atributos",
            lifelines: [
                { nombre: "c: Consultor", tipo: "Actor", pos: 80 },
                { nombre: "map: LeafletMap", tipo: "Sequence", pos: 250 },
                { nombre: "drawer: EntityDetailDrawer", tipo: "Sequence", pos: 430 },
                { nombre: "api: CapasApiController", tipo: "Sequence", pos: 620 },
                { nombre: "repo: CapaRepository", tipo: "Sequence", pos: 820 },
                { nombre: "db: SQL Server Spatial", tipo: "Sequence", pos: 1010 }
            ],
            mensajes: [
                { de: 0, a: 1, texto: "1: Clic sobre predio (lat, lon)" },
                { de: 1, a: 3, texto: "2: GET /api/capas/identificar?lat=Y&lon=X&tol=0.0001" },
                { de: 3, a: 4, texto: "3: IdentificarEntidadAsync(lat, lon)" },
                { de: 4, a: 5, texto: "4: WHERE Geometria.STIntersects(Point) = 1" },
                { de: 5, a: 4, texto: "5: Datos Atributos Prediales" },
                { de: 4, a: 3, texto: "6: FichaPredialDto" },
                { de: 3, a: 1, texto: "7: 200 OK (Atributos + WKT)" },
                { de: 1, a: 1, texto: "8: Resaltar polígono (stroke cian)" },
                { de: 1, a: 2, texto: "9: Abrir panel y renderizar ficha técnica" },
                { de: 2, a: 0, texto: "10: Visualización de datos (Área, Titular, Código)" }
            ]
        },
        {
            codigo: "CU14",
            titulo: "CU14 - Búsqueda Alfanumérica Predictiva y Enfoque en Mapa",
            lifelines: [
                { nombre: "c: Consultor", tipo: "Actor", pos: 80 },
                { nombre: "ui: SearchBar / Sidebar", tipo: "Sequence", pos: 250 },
                { nombre: "api: BusquedaApiController", tipo: "Sequence", pos: 460 },
                { nombre: "srv: BusquedaService", tipo: "Sequence", pos: 670 },
                { nombre: "repo: BusquedaRepository", tipo: "Sequence", pos: 870 },
                { nombre: "map: LeafletMap", tipo: "Sequence", pos: 1050 }
            ],
            mensajes: [
                { de: 0, a: 1, texto: "1: Escribir término (ej. 'M-12' o titular)" },
                { de: 1, a: 2, texto: "2: GET /api/busqueda/rapida?q=termino&capa=Lotes" },
                { de: 2, a: 3, texto: "3: BuscarAsync(query, capa)" },
                { de: 3, a: 4, texto: "4: ConsultarIndicesTextoYCodigos(query)" },
                { de: 4, a: 3, texto: "5: Lista de Coincidencias" },
                { de: 3, a: 2, texto: "6: List<ResultadoBusquedaDto>" },
                { de: 2, a: 1, texto: "7: Renderizar lista de resultados en sidebar" },
                { de: 0, a: 1, texto: "8: Seleccionar ítem de la lista" },
                { de: 1, a: 5, texto: "9: fitBounds(bboxGeometria) + highlight()" },
                { de: 5, a: 0, texto: "10: Zoom suave y enfoque centrado del predio" }
            ]
        },
        {
            codigo: "CU15",
            titulo: "CU15 - Medición Cartográfica (Distancia Lineal y Área)",
            lifelines: [
                { nombre: "c: Consultor", tipo: "Actor", pos: 80 },
                { nombre: "tools: PanelHerramientas", tipo: "Sequence", pos: 260 },
                { nombre: "measure: MeasurementEngine (JS)", tipo: "Sequence", pos: 480 },
                { nombre: "map: LeafletMapCanvas", tipo: "Sequence", pos: 700 }
            ],
            mensajes: [
                { de: 0, a: 1, texto: "1: Clic 'Medir Distancia' o 'Medir Área'" },
                { de: 1, a: 2, texto: "2: ActivarModoMedicion(tipo)" },
                { de: 2, a: 3, texto: "3: Cambiar cursor a Crosshair" },
                { de: 0, a: 3, texto: "4: Clics sucesivos en vértices" },
                { de: 3, a: 2, texto: "5: Evento map.click(latlng)" },
                { de: 2, a: 2, texto: "6: CalcularFormulaGeodesicaHaversine()" },
                { de: 2, a: 3, texto: "7: Trazar línea guía / polígono sombreado" },
                { de: 2, a: 3, texto: "8: Actualizar Tooltip emergente (m, km, m2, ha)" },
                { de: 0, a: 3, texto: "9: Doble clic para finalizar" },
                { de: 2, a: 0, texto: "10: Muestra resultado total fijado en pantalla" }
            ]
        },
        {
            codigo: "CU16",
            titulo: "CU16 - Geolocalización GPS y Navegación por Coordenadas",
            lifelines: [
                { nombre: "c: Usuario / Operario", tipo: "Actor", pos: 80 },
                { nombre: "ui: ControlesGpsHUD", tipo: "Sequence", pos: 260 },
                { nombre: "gps: GeolocationAPI / FlutterLocation", tipo: "Sequence", pos: 490 },
                { nombre: "map: LeafletMap", tipo: "Sequence", pos: 730 },
                { nombre: "hud: MapHUD (Coordenadas)", tipo: "Sequence", pos: 940 }
            ],
            mensajes: [
                { de: 0, a: 1, texto: "1: Pulsar 'Detectar Mi Ubicación' o 'Ir a Coords'" },
                { de: 1, a: 2, texto: "2: getCurrentPosition(highAccuracy: true)" },
                { de: 2, a: 1, texto: "3: Retorna Position (lat, lon, accuracy)" },
                { de: 1, a: 3, texto: "4: setView([lat, lon], 17)" },
                { de: 1, a: 3, texto: "5: Agregar L.circleMarker (punto azul pulsante)" },
                { de: 1, a: 4, texto: "6: ActualizarHUD(lat, lon, EPSG:4326)" },
                { de: 3, a: 0, texto: "7: Visualización de ubicación en tiempo real" }
            ]
        },
        {
            codigo: "CU17",
            titulo: "CU17 - Exportación de Información (Logs y Geometría WKT)",
            lifelines: [
                { nombre: "u: Consultor / Admin", tipo: "Actor", pos: 80 },
                { nombre: "ui: Drawer / AuditoriaView", tipo: "Sequence", pos: 260 },
                { nombre: "ctrl: ExportController / AdminController", tipo: "Sequence", pos: 490 },
                { nombre: "srv: AuditoriaService", tipo: "Sequence", pos: 710 },
                { nombre: "repo: BitacoraRepository", tipo: "Sequence", pos: 920 }
            ],
            mensajes: [
                { de: 0, a: 1, texto: "1: Clic 'Exportar CSV' o 'Copiar WKT'" },
                { de: 1, a: 2, texto: "2: SolicitudExportacion(filtros / idEntidad)" },
                { de: 2, a: 3, texto: "3: ExportarRegistrosCsvAsync(filtros)" },
                { de: 3, a: 4, texto: "4: ConsultarEventosAuditoria(filtros)" },
                { de: 4, a: 3, texto: "5: List<BitacoraDto>" },
                { de: 3, a: 3, texto: "6: SerializarStringWriterCSV(UTF8)" },
                { de: 3, a: 2, texto: "7: FileContentResult (text/csv)" },
                { de: 2, a: 1, texto: "8: Descarga de archivo 'Auditoria_SIG_2026.csv'" },
                { de: 1, a: 0, texto: "9: Notificación de descarga o copiado en portapapeles" }
            ]
        }
    ];

    for (var i = 0; i < diagramas.length; i++) {
        var d = diagramas[i];
        Session.Output("-> Creando diagrama de secuencia para: " + d.codigo);

        var diag as EA.Diagram;
        diag = pkgSec.Diagrams.AddNew(d.titulo, "Sequence");
        diag.Update();

        var elemList = [];
        for (var l = 0; l < d.lifelines.length; l++) {
            var lf = d.lifelines[l];
            var elem as EA.Element;
            elem = pkgSec.Elements.AddNew(lf.nombre, lf.tipo);
            elem.Update();
            elemList.push(elem);

            // Posicionar Lifeline en el diagrama
            var diagObj as EA.DiagramObject;
            diagObj = diag.DiagramObjects.AddNew("l=" + lf.pos + ";r=" + (lf.pos + 110) + ";t=50;b=600;", "");
            diagObj.ElementID = elem.ElementID;
            diagObj.Update();
        }

        // Crear los mensajes de secuencia
        for (var m = 0; m < d.mensajes.length; m++) {
            var msgData = d.mensajes[m];
            var src = elemList[msgData.de];
            var tgt = elemList[msgData.a];

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
    Session.Output(">> ¡Generación de Diagramas de Secuencia finalizada con éxito!");
    Session.Prompt("Se crearon exitosamente los 9 Diagramas de Secuencia en el paquete seleccionado.", promptOK);
}

main();

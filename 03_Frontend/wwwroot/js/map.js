/**
 * VisorDatosSIG 2026 — Motor Cartográfico Leaflet y Capas Espaciales
 */

const SigMap = {
    map: null,
    baseMaps: {},
    currentBaseMap: 'osm',
    vectorLayers: {},
    layerCatalog: [],
    layerOpacities: {},
    selectedFeatureLayer: null,
    identifyActive: true,
    measureMode: null, // 'distance' | 'area' | null
    measurePoints: [],
    measureLayer: null,
    userLocationLayer: null,

    // Inicialización del Mapa
    async init() {
        // 1. Crear Mapa Leaflet con renderizado Canvas acelerado por GPU
        this.map = L.map('map', {
            center: [-16.38198, -60.96915], // San Ignacio de Velasco, Santa Cruz, Bolivia (WGS 84)
            zoom: 14,
            minZoom: 3,
            maxZoom: 20,
            zoomControl: false, // Personalizado
            preferCanvas: true // ULTRA RENDIMIENTO: Renderizado en lienzo Canvas en vez de miles de nodos SVG en el DOM
        });

        // Zoom Control superior derecho
        L.control.zoom({ position: 'topright' }).addTo(this.map);

        // Control Flotante de Geolocalización en el Mapa (GPS si está permitido)
        const puedeGps = window.SIG_USER_PERMISSIONS ? window.SIG_USER_PERMISSIONS.puedeGps : true;
        if (puedeGps) {
            const LocationControl = L.Control.extend({
                options: { position: 'topright' },
                onAdd: () => {
                    const container = L.DomUtil.create('div', 'leaflet-bar');
                    const btn = L.DomUtil.create('a', 'sig-map-action-btn', container);
                    btn.id = 'mapLocateBtn';
                    btn.href = '#';
                    btn.title = 'Ir a mi ubicación actual (GPS)';
                    btn.setAttribute('role', 'button');
                    btn.setAttribute('aria-label', 'Centrar en mi ubicación');
                    btn.innerHTML = '<i class="bi bi-crosshair2"></i>';

                    L.DomEvent.disableClickPropagation(container);
                    L.DomEvent.disableScrollPropagation(container);
                    L.DomEvent.on(btn, 'click', (e) => {
                        L.DomEvent.preventDefault(e);
                        this.locateUser({ autoZoom: true, showToastMsg: true });
                    });

                    return container;
                }
            });
            this.map.addControl(new LocationControl());
        }

        L.control.scale({ position: 'bottomleft', metric: true, imperial: false }).addTo(this.map);

        // 2. Capas Base y Panes Cartográficos Jerárquicos (OGC Standard Z-Index)
        this.initBaseMaps();
        this.initCartographicPanes();

        // 3. Capas de Mediciones y Ubicación de Usuario
        this.measureLayer = L.featureGroup().addTo(this.map);
        this.userLocationLayer = L.featureGroup().addTo(this.map);

        // 4. Configurar Eventos del Mapa y Controles UI inmediatamente (sin bloquearse por red)
        this.bindEvents();
        this.bindUIControls();

        // 5. Cargar Catálogo de Capas desde el API y sincronizar geometrías en paralelo
        try {
            await this.loadCatalog();
            await this.reloadAllLayers(true, false);
        } catch (e) {
            console.error('Error al inicializar catálogo o capas:', e);
        }

        // 6. Inicializar ubicación de usuario si tiene permiso
        if (puedeGps) {
            this.locateUser({ autoZoom: false, showToastMsg: false });
        }

        // 7. Forzar recalculo de dimensiones del mapa tras renderizado del DOM en móviles y desktop
        setTimeout(() => {
            if (this.map) this.map.invalidateSize();
        }, 150);
        setTimeout(() => {
            if (this.map) this.map.invalidateSize();
        }, 500);
    },

    initCartographicPanes() {
        // Jerarquía cartográfica estandarizada:
        // Polígonos base (410) -> Polígonos de predios (420) -> Redes y líneas (430) -> Puntos e hitos (440) -> Resaltes (450)
        const panesConfig = [
            { id: 'pane_manzanas', zIndex: 410 },
            { id: 'pane_lotes', zIndex: 420 },
            { id: 'pane_vias', zIndex: 430 },
            { id: 'pane_codigosfijos', zIndex: 440 },
            { id: 'pane_highlight', zIndex: 450 }
        ];

        panesConfig.forEach(p => {
            if (!this.map.getPane(p.id)) {
                const pane = this.map.createPane(p.id);
                pane.style.zIndex = p.zIndex;
            }
        });
    },

    getPaneForLayer(nombreCapa, tipoGeometria = null) {
        const lower = (nombreCapa || '').trim().toLowerCase();
        if (lower.includes('manzana')) return 'pane_manzanas';
        if (lower.includes('lote')) return 'pane_lotes';
        if (lower.includes('via') || lower.includes('eje') || lower.includes('red')) return 'pane_vias';
        if (lower.includes('codigo') || lower.includes('hito') || lower.includes('punto')) return 'pane_codigosfijos';

        if (tipoGeometria === 'Point' || tipoGeometria === 'MultiPoint') return 'pane_codigosfijos';
        if (tipoGeometria === 'LineString' || tipoGeometria === 'MultiLineString') return 'pane_vias';
        if (tipoGeometria === 'Polygon' || tipoGeometria === 'MultiPolygon') return 'pane_lotes';

        return 'overlayPane';
    },

    initBaseMaps() {
        this.baseMaps.osm = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19
        });

        this.baseMaps.positron = L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 20
        });

        this.baseMaps.dark = L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 20
        });

        this.baseMaps.satellite = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
            attribution: 'Tiles &copy; Esri &mdash; Source: Esri, i-cubed, USDA, USGS, AEX, GeoEye, Getmapping, Aerogrid, IGN, IGP, UPR-EGP',
            maxZoom: 19
        });

        // Activar OSM por defecto
        this.baseMaps.osm.addTo(this.map);
    },

    setBaseMap(name) {
        if (!this.baseMaps[name] || this.currentBaseMap === name) return;

        if (this.currentBaseMap && this.baseMaps[this.currentBaseMap] && this.map.hasLayer(this.baseMaps[this.currentBaseMap])) {
            this.map.removeLayer(this.baseMaps[this.currentBaseMap]);
        }

        this.baseMaps[name].addTo(this.map);
        if (typeof this.baseMaps[name].bringToBack === 'function') {
            this.baseMaps[name].bringToBack();
        }
        this.currentBaseMap = name;

        document.querySelectorAll('.sig-basemap-card').forEach(c => {
            c.classList.toggle('active', c.dataset.basemap === name);
        });
    },

    async loadCatalog() {
        try {
            const res = await fetch('/api/capas');
            if (res.ok) {
                this.layerCatalog = await res.json();
                this.layerCatalog.forEach(c => {
                    this.layerOpacities[c.nombre] = c.opacidad;
                });
            }
        } catch (e) {
            console.error('Error al cargar catálogo de capas:', e);
        }
    },

    async reloadAllLayers(autoFit = false, forceRefresh = false) {
        if (forceRefresh) {
            // Limpieza completa de capas previas si se solicitó refresco forzado
            for (const key in this.vectorLayers) {
                if (this.vectorLayers[key] && this.map.hasLayer(this.vectorLayers[key])) {
                    this.map.removeLayer(this.vectorLayers[key]);
                }
            }
            this.vectorLayers = {};
        }

        // Carga concurrente en paralelo de todas las capas
        const loadPromises = this.layerCatalog.map(async (capa) => {
            const chk = document.getElementById(`chk_${capa.nombre}`);
            const visible = chk ? chk.checked : true;
            if (visible) {
                return await this.loadLayerGeoJson(capa.nombre, null, forceRefresh);
            } else {
                if (this.vectorLayers[capa.nombre] && this.map.hasLayer(this.vectorLayers[capa.nombre])) {
                    this.map.removeLayer(this.vectorLayers[capa.nombre]);
                }
                return null;
            }
        });

        const loadedLayers = await Promise.all(loadPromises);

        if (autoFit) {
            let combinedBounds = L.latLngBounds([]);
            for (const layer of loadedLayers) {
                if (layer && layer.getBounds && typeof layer.getLayers === 'function' && layer.getLayers().length > 0) {
                    const b = layer.getBounds();
                    if (b && b.isValid && b.isValid()) {
                        combinedBounds.extend(b);
                    }
                }
            }
            if (combinedBounds.isValid()) {
                this.map.fitBounds(combinedBounds, { padding: [40, 40], maxZoom: 18 });
            }
        }
    },

    async loadLayerGeoJson(nombreCapa, bbox, forceRefresh = false) {
        try {
            // Si la capa ya está en memoria y no es un refresco forzado ni filtro bbox dinámico:
            if (!forceRefresh && !bbox && this.vectorLayers[nombreCapa]) {
                const existing = this.vectorLayers[nombreCapa];
                if (!this.map.hasLayer(existing)) {
                    existing.addTo(this.map);
                }
                return existing;
            }

            const url = bbox
                ? `/api/capas/${nombreCapa}/geojson?bbox=${bbox}&limite=5000${forceRefresh ? '&refresh=true&_t=' + Date.now() : ''}`
                : `/api/capas/${nombreCapa}/geojson${forceRefresh ? '?refresh=true&_t=' + Date.now() : ''}`;
            const res = await fetch(url);
            if (!res.ok) return null;

            const geojson = await res.json();
            const config = this.layerCatalog.find(c => c.nombre.toLowerCase() === nombreCapa.toLowerCase());
            const opacity = this.layerOpacities[nombreCapa] !== undefined ? this.layerOpacities[nombreCapa] : (config ? config.opacidad : 0.7);

            // Remover capa previa si existe en el mapa
            if (this.vectorLayers[nombreCapa] && this.map.hasLayer(this.vectorLayers[nombreCapa])) {
                this.map.removeLayer(this.vectorLayers[nombreCapa]);
            }

            const targetPane = this.getPaneForLayer(nombreCapa, config ? config.tipoGeometria : null);

            const layerGroup = L.geoJSON(geojson, {
                pane: targetPane,
                style: (feature) => {
                    return {
                        pane: targetPane,
                        fillColor: config ? config.colorFondo : '#3388ff',
                        weight: config ? config.grosorBorde : 2,
                        opacity: 1,
                        color: config ? config.colorBorde : '#1e3a8a',
                        fillOpacity: opacity
                    };
                },
                pointToLayer: (feature, latlng) => {
                    const color = config ? config.colorFondo : '#ef4444';
                    const borde = config ? config.colorBorde : '#ffffff';
                    return L.circleMarker(latlng, {
                        pane: targetPane,
                        radius: 7,
                        fillColor: color,
                        color: borde,
                        weight: 2,
                        opacity: 1,
                        fillOpacity: opacity
                    });
                },
                onEachFeature: (feature, layer) => {
                    this.bindFeatureEvents(feature, layer, nombreCapa, config);
                }
            });

            this.vectorLayers[nombreCapa] = layerGroup;
            layerGroup.addTo(this.map);

            const count = geojson.features ? geojson.features.length : 0;
            if (count > 0) {
                console.log(`[SIG Engine] Capa '${nombreCapa}' cargada en [${targetPane}]: ${count} elementos renderizados.`);
            } else {
                console.warn(`[SIG Engine] Capa '${nombreCapa}' cargada con 0 elementos. Verifique si la tabla correspondiente en la Base de Datos está poblada para esta extensión geográfica.`);
            }
            return layerGroup;
        } catch (e) {
            console.error(`[SIG Engine] Error al cargar GeoJSON de capa ${nombreCapa}:`, e);
            return null;
        }
    },

    bindFeatureEvents(feature, layer, nombreCapa, config) {
        layer.on({
            mouseover: (e) => {
                const target = e.target;
                target.setStyle({
                    weight: 3,
                    color: '#38bdf8',
                    fillOpacity: Math.min(1.0, (this.layerOpacities[nombreCapa] || 0.7) + 0.2)
                });
                if (!L.Browser.ie && !L.Browser.opera && !L.Browser.edge) {
                    target.bringToFront();
                }
            },
            mouseout: (e) => {
                if (this.selectedFeatureLayer !== e.target) {
                    if (this.vectorLayers[nombreCapa]) {
                        this.vectorLayers[nombreCapa].resetStyle(e.target);
                    }
                }
            },
            click: (e) => {
                L.DomEvent.stopPropagation(e);
                this.selectFeature(feature, layer, nombreCapa);
            }
        });
    },

    selectFeature(feature, layer, nombreCapa) {
        // Reset estilo anterior
        if (this.selectedFeatureLayer && this.selectedFeatureLayer.feature) {
            const prevCapa = this.selectedFeatureLayer._sigCapa;
            if (prevCapa && this.vectorLayers[prevCapa]) {
                this.vectorLayers[prevCapa].resetStyle(this.selectedFeatureLayer);
            }
        }

        this.selectedFeatureLayer = layer;
        layer._sigCapa = nombreCapa;

        layer.setStyle({
            weight: 4,
            color: '#f59e0b',
            fillColor: '#fef08a',
            fillOpacity: 0.85
        });

        const id = feature.id || (feature.properties ? (feature.properties.idLote || feature.properties.idManzana || feature.properties.idVia || feature.properties.idCodigoFijo) : null);

        // Popup rápido
        const title = feature.properties ? (feature.properties.codigoLote || feature.properties.codigoManzana || feature.properties.codigoVia || feature.properties.codigoFijo || `ID ${id}`) : `Entidad ${id}`;
        const subtitle = feature.properties ? (feature.properties.titular || feature.properties.nombre || feature.properties.barrio || feature.properties.descripcion || '') : '';

        const popupContent = `
            <div class="sig-popup-header">
                <span class="badge bg-primary mb-1">${nombreCapa}</span>
                <h6>${title}</h6>
                ${subtitle ? `<small class="text-muted">${subtitle}</small>` : ''}
            </div>
            <div class="sig-popup-body">
                <button class="btn btn-sm btn-primary sig-popup-btn" onclick="SigMap.openEntityDrawer('${nombreCapa}', ${id})">
                    <i class="bi bi-card-text me-1"></i> Ver Ficha Completa
                </button>
            </div>
        `;

        layer.bindPopup(popupContent).openPopup();

        // Abrir panel lateral automáticamente
        if (id) {
            this.openEntityDrawer(nombreCapa, id);
        }
    },

    async openEntityDrawer(nombreCapa, id) {
        const drawer = document.getElementById('entityDetailDrawer');
        const titleElem = document.getElementById('drawerEntityTitle');
        const subElem = document.getElementById('drawerEntitySubtitle');
        const contentElem = document.getElementById('drawerEntityContent');

        if (window.innerWidth < 768) {
            this.closeSidebar();
        }

        drawer.classList.add('open');
        contentElem.innerHTML = '<div class="text-center py-4 text-muted"><div class="spinner-border spinner-border-sm text-primary me-2"></div>Cargando ficha técnica...</div>';

        try {
            const res = await fetch(`/api/capas/${nombreCapa}/${id}`);
            if (!res.ok) throw new Error('No se pudo obtener el detalle');

            const data = await res.json();
            titleElem.textContent = `${data.capa}: ${data.codigo}`;
            subElem.textContent = data.titulo || nombreCapa;

            let html = '<div class="sig-attrs-container">';
            for (const [key, val] of Object.entries(data.atributos)) {
                if (val !== null && val !== undefined && val !== '') {
                    html += `
                        <div class="sig-attr-row">
                            <span class="sig-attr-label">${key}</span>
                            <span class="sig-attr-value">${val}</span>
                        </div>
                    `;
                }
            }

            if (data.lonCentro && data.latCentro) {
                html += `
                    <div class="sig-attr-row">
                        <span class="sig-attr-label">Centroide WGS84</span>
                        <span class="sig-attr-value font-monospace small">${data.lonCentro.toFixed(6)}, ${data.latCentro.toFixed(6)}</span>
                    </div>
                `;
            }

            html += '</div>';
            contentElem.innerHTML = html;

            // Configurar botones de footer
            document.getElementById('btnZoomToEntity').onclick = () => {
                if (data.lonCentro && data.latCentro) {
                    this.map.flyTo([data.latCentro, data.lonCentro], 18, { animate: true, duration: 1.2 });
                }
            };

            document.getElementById('btnCopyEntityWkt').onclick = () => {
                if (data.wkt) {
                    navigator.clipboard.writeText(data.wkt);
                    showToast('Copiado', 'Geometría WKT copiada al portapapeles.', 'success');
                }
            };
        } catch (err) {
            contentElem.innerHTML = `<div class="alert alert-danger small">${err.message}</div>`;
        }
    },

    bindEvents() {
        // Actualizar HUD de coordenadas bajo el mouse
        this.map.on('mousemove', (e) => {
            const hudCoords = document.getElementById('hudCoords');
            if (hudCoords) {
                hudCoords.innerHTML = `<i class="bi bi-geo-alt-fill text-danger me-1"></i>Lon: ${e.latlng.lng.toFixed(5)} | Lat: ${e.latlng.lat.toFixed(5)}`;
            }
        });

        this.map.on('zoomend', () => {
            const hudZoom = document.getElementById('hudZoom');
            if (hudZoom) {
                hudZoom.textContent = `Zoom: ${this.map.getZoom()}`;
            }
        });

        // Búsqueda espacial por clic (Identificar) o Mediciones
        this.map.on('click', async (e) => {
            if (this.measureMode) {
                this.handleMeasureClick(e.latlng);
                return;
            }

            if (this.identifyActive) {
                await this.identifyAtPoint(e.latlng.lng, e.latlng.lat);
            }
        });

        // Recargar capas según BBox al mover el mapa (con debounce de 400ms)
        let moveTimeout;
        this.map.on('moveend', () => {
            clearTimeout(moveTimeout);
            moveTimeout = setTimeout(() => {
                this.reloadAllLayers();
            }, 400);
        });

        // Manejador de redimensionamiento de ventana y cambio de orientación en móviles
        let resizeTimer;
        window.addEventListener('resize', () => {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(() => {
                if (this.map) {
                    this.map.invalidateSize();
                }
            }, 120);
        });

        window.addEventListener('orientationchange', () => {
            setTimeout(() => {
                if (this.map) {
                    this.map.invalidateSize();
                }
            }, 250);
        });
    },

    async identifyAtPoint(lon, lat) {
        try {
            const res = await fetch(`/api/busqueda/identificar?lon=${lon}&lat=${lat}&tolerancia=5`);
            if (!res.ok) return;

            const items = await res.json();
            if (items.length === 0) {
                showToast('Identificación', 'No se encontraron entidades en este punto.', 'info');
                return;
            }

            const first = items[0];
            this.openEntityDrawer(first.capa, first.idEntidad);
        } catch (e) {
            console.error('Error al identificar por punto:', e);
        }
    },

    openSidebarTab(tabName) {
        const sidebar = document.getElementById('sigSidebar');
        const btnToggleSidebar = document.getElementById('btnToggleSidebar');
        const backdrop = document.getElementById('sigSidebarBackdrop');
        const drawer = document.getElementById('entityDetailDrawer');

        // Si el drawer de detalles está abierto en móvil, cerrarlo para evitar sobreposición
        if (window.innerWidth < 768 && drawer && drawer.classList.contains('open')) {
            drawer.classList.remove('open');
        }

        const isCollapsed = sidebar && sidebar.classList.contains('collapsed');
        const currentActiveTab = document.querySelector('.sig-sidebar .nav-link.active');
        const isSameTab = currentActiveTab && currentActiveTab.getAttribute('data-bs-target') === `#${tabName}`;

        // Si se presiona el mismo botón en móvil y el panel está abierto, cerrarlo (comportamiento toggle)
        if (!isCollapsed && isSameTab && window.innerWidth < 768) {
            this.closeSidebar();
            return;
        }

        if (sidebar) {
            sidebar.classList.remove('collapsed');
        }
        if (btnToggleSidebar) {
            btnToggleSidebar.classList.remove('collapsed-mode');
        }
        if (backdrop && window.innerWidth < 768) {
            backdrop.classList.add('active');
        }

        const tabBtn = document.getElementById(`${tabName}-btn`);
        if (tabBtn && typeof bootstrap !== 'undefined') {
            const tab = bootstrap.Tab.getOrCreateInstance(tabBtn);
            tab.show();
        }

        this.updateMobileDockState(tabName);

        if (tabName === 'tab-busqueda') {
            setTimeout(() => {
                const searchInp = document.getElementById('sidebarSearchInput');
                if (searchInp) searchInp.focus();
            }, 300);
        }

        setTimeout(() => {
            if (this.map) this.map.invalidateSize();
        }, 320);
    },

    closeSidebar() {
        const sidebar = document.getElementById('sigSidebar');
        const btnToggleSidebar = document.getElementById('btnToggleSidebar');
        const backdrop = document.getElementById('sigSidebarBackdrop');

        if (sidebar) {
            sidebar.classList.add('collapsed');
        }
        if (btnToggleSidebar) {
            btnToggleSidebar.classList.add('collapsed-mode');
        }
        if (backdrop) {
            backdrop.classList.remove('active');
        }

        // Desactivar items en el dock móvil
        document.querySelectorAll('.sig-dock-item').forEach(btn => {
            if (btn.id !== 'dockBtnGps' && btn.id !== 'dockBtnLegend') {
                btn.classList.remove('active');
            }
        });

        setTimeout(() => {
            if (this.map) this.map.invalidateSize();
        }, 320);
    },

    toggleSidebar() {
        const sidebar = document.getElementById('sigSidebar');
        if (!sidebar) return;
        if (sidebar.classList.contains('collapsed')) {
            const activeTab = document.querySelector('.sig-sidebar .nav-link.active');
            const targetId = activeTab ? activeTab.getAttribute('data-bs-target')?.replace('#', '') : 'tab-capas';
            this.openSidebarTab(targetId || 'tab-capas');
        } else {
            this.closeSidebar();
        }
    },

    updateMobileDockState(tabName) {
        document.querySelectorAll('.sig-dock-item').forEach(btn => {
            if (btn.id !== 'dockBtnGps' && btn.id !== 'dockBtnLegend') {
                btn.classList.remove('active');
            }
        });
        if (tabName === 'tab-capas') {
            document.getElementById('dockBtnCapas')?.classList.add('active');
        } else if (tabName === 'tab-busqueda') {
            document.getElementById('dockBtnBusqueda')?.classList.add('active');
        } else if (tabName === 'tab-herramientas') {
            document.getElementById('dockBtnHerramientas')?.classList.add('active');
        }
    },

    toggleLegend() {
        const legendBody = document.getElementById('legendBody');
        const legendChevron = document.getElementById('legendChevron');
        const floatingLegend = document.getElementById('sigFloatingLegend');
        if (!legendBody || !floatingLegend) return;

        const isMobile = window.innerWidth < 768;
        if (isMobile) {
            const isExpanded = floatingLegend.classList.contains('expanded');
            const willExpand = !isExpanded;
            floatingLegend.classList.toggle('expanded', willExpand);
            legendBody.style.display = willExpand ? 'block' : 'none';
            if (legendChevron) {
                legendChevron.className = willExpand ? 'bi bi-chevron-down' : 'bi bi-chevron-up';
            }
            document.getElementById('dockBtnLegend')?.classList.toggle('active', willExpand);
        } else {
            const isHidden = legendBody.style.display === 'none' || window.getComputedStyle(legendBody).display === 'none';
            legendBody.style.display = isHidden ? 'block' : 'none';
            if (legendChevron) {
                legendChevron.className = isHidden ? 'bi bi-chevron-down' : 'bi bi-chevron-up';
            }
            floatingLegend.classList.toggle('expanded', isHidden);
            document.getElementById('dockBtnLegend')?.classList.toggle('active', isHidden);
        }
    },

    bindUIControls() {
        // Toggle Sidebar Botón Flotante
        const btnToggleSidebar = document.getElementById('btnToggleSidebar');
        const sidebar = document.getElementById('sigSidebar');
        if (btnToggleSidebar && sidebar) {
            // En móviles iniciar colapsado para ver el mapa despejado
            if (window.innerWidth < 768) {
                sidebar.classList.add('collapsed');
                btnToggleSidebar.classList.add('collapsed-mode');
            }

            btnToggleSidebar.addEventListener('click', () => {
                this.toggleSidebar();
            });
        }

        // Botón Cerrar en el Header del Sidebar
        const btnCloseSidebar = document.getElementById('btnCloseSidebar');
        if (btnCloseSidebar) {
            btnCloseSidebar.addEventListener('click', () => {
                this.closeSidebar();
            });
        }

        // Backdrop Móvil: Cierra el sidebar al tocar fuera
        const sidebarBackdrop = document.getElementById('sigSidebarBackdrop');
        if (sidebarBackdrop) {
            sidebarBackdrop.addEventListener('click', () => {
                this.closeSidebar();
            });
        }

        // Escuchar clics directos en pestañas del Sidebar para sincronizar dock móvil
        document.querySelectorAll('#sidebarTab .nav-link').forEach(tabBtn => {
            tabBtn.addEventListener('shown.bs.tab', (e) => {
                const targetId = e.target.getAttribute('data-bs-target')?.replace('#', '');
                if (targetId) {
                    this.updateMobileDockState(targetId);
                }
            });
        });

        // Sincronización con Cambio de Tema (Claro / Oscuro)
        window.addEventListener('sig-theme-changed', (e) => {
            const theme = e.detail?.theme || (document.documentElement.getAttribute('data-theme') || 'dark');
            if (theme === 'light' && this.currentBaseMap === 'dark') {
                this.setBaseMap('positron');
            } else if (theme === 'dark' && this.currentBaseMap === 'positron') {
                this.setBaseMap('dark');
            }
        });

        // Close Detail Drawer
        const btnCloseDrawer = document.getElementById('btnCloseDetailDrawer');
        const drawer = document.getElementById('entityDetailDrawer');
        if (btnCloseDrawer && drawer) {
            btnCloseDrawer.addEventListener('click', () => {
                drawer.classList.remove('open');
            });
        }

        // Handle de arrastre para cerrar drawer en móvil
        const drawerHandle = document.getElementById('drawerHandle');
        if (drawerHandle && drawer) {
            drawerHandle.addEventListener('click', () => {
                drawer.classList.remove('open');
            });
        }

        // Toggle Leyenda
        const btnToggleLegend = document.getElementById('btnToggleLegend');
        if (btnToggleLegend) {
            btnToggleLegend.addEventListener('click', () => {
                this.toggleLegend();
            });
        }

        // Switches de Capas (Activación/Desactivación instantánea en 0ms con Retención en Memoria)
        document.querySelectorAll('.sig-layer-toggle').forEach(chk => {
            chk.addEventListener('change', async (e) => {
                const capa = e.target.dataset.layerName;
                if (e.target.checked) {
                    if (this.vectorLayers[capa]) {
                        // Instantáneo: 0 ms (ya instanciado en memoria RAM)
                        this.vectorLayers[capa].addTo(this.map);
                    } else {
                        // Carga inicial asíncrona optimizada
                        await this.loadLayerGeoJson(capa, null);
                    }
                } else {
                    if (this.vectorLayers[capa] && this.map.hasLayer(this.vectorLayers[capa])) {
                        // Instantáneo: Ocultar sin destruir datos
                        this.map.removeLayer(this.vectorLayers[capa]);
                    }
                }
            });
        });

        // Sliders de Opacidad
        document.querySelectorAll('.sig-opacity-slider').forEach(slider => {
            slider.addEventListener('input', (e) => {
                const capa = e.target.dataset.layerName;
                const val = parseInt(e.target.value, 10);
                const opacity = val / 100.0;
                this.layerOpacities[capa] = opacity;

                const label = document.getElementById(`val_${capa}`);
                if (label) label.textContent = `${val}%`;

                if (this.vectorLayers[capa]) {
                    this.vectorLayers[capa].setStyle({ fillOpacity: opacity });
                }
            });
        });

        // Botones de Zoom a Capa Específica
        document.querySelectorAll('.sig-btn-zoom-layer').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const capa = btn.dataset.layerName;
                this.zoomToLayer(capa);
            });
        });

        // Galería Mapas Base
        document.querySelectorAll('.sig-basemap-card').forEach(card => {
            card.addEventListener('click', () => {
                this.setBaseMap(card.dataset.basemap);
            });
        });

        // Botón Recargar Capas (Forzar sincronización limpia desde Base de Datos)
        const btnReload = document.getElementById('btnReloadAllLayers');
        if (btnReload) {
            btnReload.addEventListener('click', async () => {
                btnReload.classList.add('rotate-anim');
                await this.reloadAllLayers(false, true);
                setTimeout(() => btnReload.classList.remove('rotate-anim'), 600);
                showToast('Capas Sincronizadas', 'Se han actualizado las geometrías desde el servidor.', 'success');
            });
        }

        // Botón Restablecer Vista
        const btnReset = document.getElementById('btnResetView');
        if (btnReset) {
            btnReset.addEventListener('click', () => {
                this.map.flyTo([-16.38198, -60.96915], 14);
            });
        }

        // Botón Detectar Mi Ubicación en Panel Herramientas
        const btnLocate = document.getElementById('btnLocateUser');
        if (btnLocate) {
            btnLocate.addEventListener('click', () => {
                this.locateUser({ autoZoom: true, showToastMsg: true });
            });
        }

        // Ir a Coordenadas
        const btnGoTo = document.getElementById('btnGoToCoords');
        if (btnGoTo) {
            btnGoTo.addEventListener('click', () => {
                const lon = parseFloat(document.getElementById('inputGoToLon').value);
                const lat = parseFloat(document.getElementById('inputGoToLat').value);

                if (isNaN(lon) || isNaN(lat)) {
                    showToast('Coordenadas Inválidas', 'Por favor ingrese valores numéricos válidos.', 'warning');
                    return;
                }

                L.marker([lat, lon]).addTo(this.map).bindPopup(`<b>Punto Localizado</b><br>Lon: ${lon}<br>Lat: ${lat}`).openPopup();
                this.map.flyTo([lat, lon], 18);
            });
        }

        // Herramienta Identificar Toggle
        const btnToolIdentify = document.getElementById('btnToolIdentify');
        const badgeIdentify = document.getElementById('badgeIdentifyStatus');
        if (btnToolIdentify && badgeIdentify) {
            btnToolIdentify.addEventListener('click', () => {
                this.identifyActive = !this.identifyActive;
                this.measureMode = null;
                btnToolIdentify.classList.toggle('active', this.identifyActive);
                badgeIdentify.textContent = this.identifyActive ? 'Activo' : 'Inactivo';
                badgeIdentify.className = this.identifyActive ? 'badge bg-success' : 'badge bg-secondary';
            });
        }

        // Herramientas Medición
        const btnMeasureDist = document.getElementById('btnToolMeasureDistance');
        const btnMeasureArea = document.getElementById('btnToolMeasureArea');

        if (btnMeasureDist) {
            btnMeasureDist.addEventListener('click', () => {
                this.measureMode = this.measureMode === 'distance' ? null : 'distance';
                this.identifyActive = this.measureMode === null;
                this.measurePoints = [];
                this.measureLayer.clearLayers();
                btnMeasureDist.classList.toggle('active', this.measureMode === 'distance');
                if (btnMeasureArea) btnMeasureArea.classList.remove('active');
                showToast('Medir Distancia', this.measureMode ? 'Haga clic en el mapa para trazar puntos.' : 'Modo medición desactivado.', 'info');
            });
        }

        if (btnMeasureArea) {
            btnMeasureArea.addEventListener('click', () => {
                this.measureMode = this.measureMode === 'area' ? null : 'area';
                this.identifyActive = this.measureMode === null;
                this.measurePoints = [];
                this.measureLayer.clearLayers();
                btnMeasureArea.classList.toggle('active', this.measureMode === 'area');
                if (btnMeasureDist) btnMeasureDist.classList.remove('active');
                showToast('Medir Área', this.measureMode ? 'Haga clic en el mapa para delimitar el polígono.' : 'Modo medición desactivado.', 'info');
            });
        }
    },

    handleMeasureClick(latlng) {
        this.measurePoints.push(latlng);
        L.circleMarker(latlng, { radius: 5, color: '#f59e0b', fillColor: '#fff', fillOpacity: 1 }).addTo(this.measureLayer);

        if (this.measureMode === 'distance') {
            if (this.measurePoints.length >= 2) {
                const polyline = L.polyline(this.measurePoints, { color: '#f59e0b', weight: 3, dashArray: '5, 5' }).addTo(this.measureLayer);
                let totalDist = 0;
                for (let i = 0; i < this.measurePoints.length - 1; i++) {
                    totalDist += this.measurePoints[i].distanceTo(this.measurePoints[i + 1]);
                }
                const distStr = totalDist > 1000 ? `${(totalDist / 1000).toFixed(2)} km` : `${totalDist.toFixed(1)} m`;
                L.popup().setLatLng(latlng).setContent(`<b>Distancia Total:</b> ${distStr}`).openOn(this.map);
            }
        } else if (this.measureMode === 'area') {
            if (this.measurePoints.length >= 3) {
                const polygon = L.polygon(this.measurePoints, { color: '#38bdf8', fillColor: '#38bdf8', fillOpacity: 0.3 }).addTo(this.measureLayer);
                // Aproximación de área esférica
                const areaM2 = this.calculatePolygonArea(this.measurePoints);
                const areaStr = areaM2 > 10000 ? `${(areaM2 / 10000).toFixed(2)} ha (${areaM2.toFixed(1)} m²)` : `${areaM2.toFixed(1)} m²`;
                L.popup().setLatLng(latlng).setContent(`<b>Área Calculada:</b> ${areaStr}`).openOn(this.map);
            }
        }
    },

    calculatePolygonArea(points) {
        if (points.length < 3) return 0;
        let total = 0;
        const radius = 6378137;
        for (let i = 0; i < points.length; i++) {
            const p1 = points[i];
            const p2 = points[(i + 1) % points.length];
            const rad1 = p1.lat * (Math.PI / 180);
            const rad2 = p2.lat * (Math.PI / 180);
            const deltaLon = (p2.lng - p1.lng) * (Math.PI / 180);
            total += deltaLon * (2 + Math.sin(rad1) + Math.sin(rad2));
        }
        return Math.abs((total * radius * radius) / 2.0);
    },

    // Módulo de Geolocalización de Usuario (W3C Geolocation API)
    locateUser(options = { autoZoom: true, showToastMsg: true }) {
        if (!("geolocation" in navigator)) {
            if (options.showToastMsg && typeof showToast === 'function') {
                showToast('Geolocalización No Soportada', 'Tu navegador o dispositivo no soporta la API de geolocalización.', 'danger');
            }
            return;
        }

        const btnText = document.getElementById('btnLocateText');
        const mapLocateBtn = document.getElementById('mapLocateBtn');
        const badgeGps = document.getElementById('badgeGpsAccuracy');

        if (btnText) btnText.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span> Buscando GPS...';
        if (mapLocateBtn) mapLocateBtn.classList.add('locating');

        const geoOptions = {
            enableHighAccuracy: true,
            timeout: 10000,
            maximumAge: 30000
        };

        navigator.geolocation.getCurrentPosition(
            (position) => {
                const lat = position.coords.latitude;
                const lon = position.coords.longitude;
                const accuracy = position.coords.accuracy || 15; // en metros

                if (this.userLocationLayer) {
                    this.userLocationLayer.clearLayers();
                } else {
                    this.userLocationLayer = L.featureGroup().addTo(this.map);
                }

                // 1. Círculo de radio de precisión
                L.circle([lat, lon], {
                    radius: accuracy,
                    color: '#0284c7',
                    fillColor: '#38bdf8',
                    fillOpacity: 0.15,
                    weight: 1.5
                }).addTo(this.userLocationLayer);

                // 2. Marcador con icono pulsante de usuario
                const pulseIcon = L.divIcon({
                    className: 'sig-user-pulse-icon',
                    html: '<div class="sig-user-pulse-wave"></div><div class="sig-user-pulse-dot"></div>',
                    iconSize: [20, 20],
                    iconAnchor: [10, 10]
                });

                const userMarker = L.marker([lat, lon], { icon: pulseIcon }).addTo(this.userLocationLayer);

                if (badgeGps) {
                    badgeGps.textContent = `±${Math.round(accuracy)}m`;
                    badgeGps.className = 'badge bg-success-subtle text-success';
                }

                // Centrar de forma directa y limpia en la ubicación
                if (options.autoZoom) {
                    this.map.flyTo([lat, lon], 17, { duration: 1.2 });
                }

                if (btnText) btnText.innerHTML = '<i class="bi bi-check2-circle text-success me-1"></i> Mi Ubicación';
                if (mapLocateBtn) mapLocateBtn.classList.remove('locating');
            },
            (error) => {
                let errorMsg = 'No fue posible acceder a tu ubicación.';
                switch (error.code) {
                    case error.PERMISSION_DENIED:
                        errorMsg = 'Permiso de ubicación denegado por el usuario o navegador.';
                        break;
                    case error.POSITION_UNAVAILABLE:
                        errorMsg = 'Información de GPS no disponible en el dispositivo.';
                        break;
                    case error.TIMEOUT:
                        errorMsg = 'Tiempo de espera agotado al consultar GPS.';
                        break;
                }

                if (options.showToastMsg && typeof showToast === 'function') {
                    showToast('Geolocalización', errorMsg, 'warning');
                }

                if (btnText) btnText.innerHTML = '<i class="bi bi-crosshair2 me-1"></i> Detectar Mi Ubicación';
                if (mapLocateBtn) mapLocateBtn.classList.remove('locating');
            },
            geoOptions
        );
    },

    // Centrar vista en los límites de una capa vectorial específica
    async zoomToLayer(nombreCapa) {
        let layer = this.vectorLayers[nombreCapa];
        if (!layer || !this.map.hasLayer(layer)) {
            // Si la capa no está cargada o visible, cargarla y mostrarla
            const chk = document.getElementById(`chk_${nombreCapa}`);
            if (chk) chk.checked = true;
            layer = await this.loadLayerGeoJson(nombreCapa, null);
        }

        if (layer && typeof layer.getBounds === 'function') {
            try {
                const b = layer.getBounds();
                if (b && b.isValid && b.isValid()) {
                    this.map.fitBounds(b, { padding: [50, 50], maxZoom: 18 });
                    if (typeof showToast === 'function') {
                        showToast('Capa Centrada', `Vista ajustada a la extensión de ${nombreCapa}.`, 'info');
                    }
                    return;
                }
            } catch (e) {
                console.warn('Error al calcular bounds de capa:', e);
            }
        }

        if (typeof showToast === 'function') {
            showToast('Información', `No se encontraron geometrías para la capa ${nombreCapa}.`, 'warning');
        }
    }
};

// Auto-arranque al cargar ventana
window.addEventListener('DOMContentLoaded', () => {
    SigMap.init();
});

/**
 * VisorDatosSIG 2026 — Motor de Búsqueda Espacial y Alfanumérica
 */

const SigSearch = {
    debounceTimer: null,
    lastResults: null,

    init() {
        const inputSidebar = document.getElementById('sidebarSearchInput');
        const inputNav = document.getElementById('navQuickSearchInput');
        const btnNav = document.getElementById('btnNavQuickSearch');
        const btnSidebar = document.getElementById('btnExecuteSearch');
        const btnClear = document.getElementById('btnClearSearch');
        const filterLayer = document.getElementById('searchLayerFilter');
        const btnFit = document.getElementById('btnFitSearchResults');

        if (inputSidebar) {
            inputSidebar.addEventListener('input', (e) => {
                clearTimeout(this.debounceTimer);
                this.debounceTimer = setTimeout(() => {
                    this.executeSearch(e.target.value.trim(), filterLayer ? filterLayer.value : '');
                }, 350);
            });

            inputSidebar.addEventListener('keydown', (e) => {
                if (e.key === 'Enter') {
                    clearTimeout(this.debounceTimer);
                    this.executeSearch(inputSidebar.value.trim(), filterLayer ? filterLayer.value : '');
                }
            });
        }

        if (btnSidebar && inputSidebar) {
            btnSidebar.addEventListener('click', () => {
                this.executeSearch(inputSidebar.value.trim(), filterLayer ? filterLayer.value : '');
            });
        }

        if (filterLayer && inputSidebar) {
            filterLayer.addEventListener('change', () => {
                if (inputSidebar.value.trim().length > 0) {
                    this.executeSearch(inputSidebar.value.trim(), filterLayer.value);
                }
            });
        }

        if (btnClear && inputSidebar) {
            btnClear.addEventListener('click', () => {
                inputSidebar.value = '';
                this.clearResults();
            });
        }

        if (inputNav && btnNav) {
            btnNav.addEventListener('click', () => {
                const term = inputNav.value.trim();
                if (term) {
                    // Activar tab de búsqueda en sidebar
                    const tabBtn = document.getElementById('tab-busqueda-btn');
                    if (tabBtn) {
                        const tab = new bootstrap.Tab(tabBtn);
                        tab.show();
                    }
                    if (inputSidebar) inputSidebar.value = term;
                    this.executeSearch(term, '');
                }
            });

            inputNav.addEventListener('keydown', (e) => {
                if (e.key === 'Enter') {
                    btnNav.click();
                }
            });
        }

        if (btnFit) {
            btnFit.addEventListener('click', () => {
                if (this.lastResults && this.lastResults.boundingBox) {
                    const b = this.lastResults.boundingBox;
                    SigMap.map.fitBounds([
                        [b.minLat, b.minLon],
                        [b.maxLat, b.maxLon]
                    ], { padding: [40, 40], maxZoom: 19 });
                }
            });
        }
    },

    async executeSearch(termino, capa = '') {
        const container = document.getElementById('searchResultsList');
        const countLabel = document.getElementById('lblResultsCount');
        const btnFit = document.getElementById('btnFitSearchResults');

        if (!termino || termino.length < 1) {
            this.clearResults();
            return;
        }

        container.innerHTML = '<div class="text-center py-4 text-muted"><div class="spinner-border spinner-border-sm text-warning me-2"></div>Buscando entidades...</div>';

        try {
            const url = `/api/busqueda?q=${encodeURIComponent(termino)}&capa=${encodeURIComponent(capa)}&pagina=1&registrosPorPagina=30`;
            const res = await fetch(url);
            if (!res.ok) throw new Error('Error al ejecutar búsqueda');

            const data = await res.json();
            this.lastResults = data;

            countLabel.textContent = `${data.totalResultados} resultados encontrados`;

            if (data.boundingBox) {
                btnFit.classList.remove('d-none');
            } else {
                btnFit.classList.add('d-none');
            }

            if (data.totalResultados === 0) {
                container.innerHTML = `
                    <div class="text-center text-muted p-4">
                        <i class="bi bi-exclamation-circle display-6 opacity-50 mb-2"></i>
                        <p class="small mb-0">No se encontraron registros para '<strong>${termino}</strong>'.</p>
                    </div>
                `;
                return;
            }

            let html = '';
            for (const item of data.resultados) {
                const capaBadge = item.capa === 'Lotes' ? 'bg-primary' : (item.capa === 'Manzanas' ? 'bg-warning text-dark' : (item.capa === 'Vias' ? 'bg-secondary' : 'bg-danger'));
                const highlightedDesc = this.highlightText(item.descripcion, termino);
                const highlightedCode = this.highlightText(item.codigo, termino);

                html += `
                    <div class="sig-search-result-item" onclick="SigSearch.selectResult('${item.capa}', ${item.idEntidad}, ${item.lonCentro || 'null'}, ${item.latCentro || 'null'})">
                        <div class="d-flex justify-content-between align-items-start mb-1">
                            <span class="fw-bold text-accent">${highlightedCode}</span>
                            <span class="badge ${capaBadge} small">${item.capa}</span>
                        </div>
                        <div class="small text-light mb-1">${highlightedDesc}</div>
                        ${item.detalle ? `<div class="small text-muted">${item.detalle}</div>` : ''}
                    </div>
                `;
            }

            container.innerHTML = html;

            // Auto-zoom al BBox si hay resultados
            if (data.boundingBox) {
                const b = data.boundingBox;
                SigMap.map.fitBounds([
                    [b.minLat, b.minLon],
                    [b.maxLat, b.maxLon]
                ], { padding: [40, 40], maxZoom: 18 });
            }
        } catch (err) {
            container.innerHTML = `<div class="alert alert-danger small">${err.message}</div>`;
        }
    },

    selectResult(capa, idEntidad, lon, lat) {
        if (window.innerWidth < 768 && typeof SigMap !== 'undefined' && SigMap.closeSidebar) {
            SigMap.closeSidebar();
        }

        if (lon !== null && lat !== null && !isNaN(lon) && !isNaN(lat) && typeof SigMap !== 'undefined' && SigMap.map) {
            SigMap.map.flyTo([lat, lon], 18, { animate: true, duration: 1.0 });
        }

        if (typeof SigMap !== 'undefined' && SigMap.openEntityDrawer) {
            SigMap.openEntityDrawer(capa, idEntidad);
        }
    },

    clearResults() {
        const container = document.getElementById('searchResultsList');
        const countLabel = document.getElementById('lblResultsCount');
        const btnFit = document.getElementById('btnFitSearchResults');

        if (countLabel) countLabel.textContent = '0 resultados encontrados';
        if (btnFit) btnFit.classList.add('d-none');
        if (container) {
            container.innerHTML = `
                <div class="text-center text-muted p-4">
                    <i class="bi bi-search display-6 opacity-50 mb-2"></i>
                    <p class="small mb-0">Ingrese un término para buscar por código de lote, titular, número predial, manzana o nombre de vía.</p>
                </div>
            `;
        }
    },

    highlightText(text, query) {
        if (!text || !query) return text || '';
        const regex = new RegExp(`(${query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
        return text.replace(regex, '<span class="sig-highlight-match">$1</span>');
    }
};

window.addEventListener('DOMContentLoaded', () => {
    SigSearch.init();
});

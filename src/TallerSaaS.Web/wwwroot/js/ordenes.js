(function (window, $) {
    let currentOrdenesPageSize = 20;
    let isOrdenesLoading = false;

    window.initOrdenes = function (retryCount = 0) {
        console.log("Antigravity: Initializing Orders Module... Attempt: " + (retryCount + 1));
        const tbody = document.getElementById('tbodyOrdenes');
        
        if (!tbody && retryCount < 10) {
            setTimeout(() => window.initOrdenes(retryCount + 1), 200);
            return;
        }

        if (tbody) {
            isOrdenesLoading = false; // Reset lock for fresh SPA visit
            loadOrdenes(1);
        }
    };

    // ── Data Loading ──────────────────────────────────────────────────────────
    window.loadOrdenes = function (page = 1, pageSize = null) {
        if (isOrdenesLoading) return;
        if (pageSize) currentOrdenesPageSize = pageSize;
        
        const filterEl = document.getElementById('estadoFiltro');
        const estado = filterEl ? filterEl.value : '';
        const tbody = document.getElementById('tbodyOrdenes');
        if (!tbody) return;

        // Show spinner
        tbody.innerHTML = '<tr><td colspan="6" class="text-center py-5 text-muted"><div class="spinner-border spinner-border-sm me-2"></div>Cargando...</td></tr>';
        
        let url = `/Ordenes/GetPaged?page=${page}&size=${currentOrdenesPageSize}`;
        if (estado) url += `&estado=${estado}`;

        fetch(url)
            .then(r => r.json())
            .then(data => {
                const subtitle = document.getElementById('subtitleConteo');
                if (subtitle) subtitle.innerText = `${data.totalCount} ordenes en total`;

                if (!data.data || data.data.length === 0) {
                    tbody.innerHTML = `<tr><td colspan="6" class="text-center py-5" style="color:var(--text-muted);">
                      <i class="bi bi-file-earmark-text" style="font-size:48px;opacity:.3;display:block;margin-bottom:12px;"></i>
                      No hay ordenes con este filtro.
                    </td></tr>`;
                    const paginator = document.getElementById('paginatorContainer');
                    if (paginator) paginator.innerHTML = '';
                    return;
                }

                let html = '';
                data.data.forEach(o => {
                    const fecha = new Date(o.fechaEntrada).toLocaleDateString('es-CO', {day:'2-digit', month:'2-digit', year:'2-digit'});
                    const total = o.total ? o.total.toLocaleString('en-US', {minimumFractionDigits:2, maximumFractionDigits:2}) : '0.00';
                    const numOrden = o.numeroOrden || '';
                    
                    html += `<tr>
                      <td><strong style="color:var(--primary);">${numOrden}</strong></td>
                      <td>
                        <div style="font-weight:600;font-size:14px;">${o.clienteNombre || '—'}</div>
                        <div style="font-size:12px;color:var(--text-muted);">${o.vehiculoDescripcion || '—'}</div>
                      </td>
                      <td class="d-none d-md-table-cell" style="color:var(--text-muted);">${fecha}</td>
                      <td>
                        <span class="estado-badge ${o.estadoClase || 'bg-secondary'}">${o.estadoTexto || ''}</span>
                      </td>
                      <td class="d-none d-lg-table-cell"><strong>$${total}</strong></td>
                      <td>
                        <div class="d-flex gap-1">
                          <a href="/Ordenes/Detalle/${o.id}" class="btn btn-sm btn-primary" title="Ver Detalle">
                            <i class="bi bi-eye"></i>
                          </a>
                          <a href="/Reportes/FacturaPdf?ordenId=${o.id}" target="_blank" class="btn btn-sm btn-outline-secondary" title="Descargar PDF">
                            <i class="bi bi-file-earmark-pdf"></i>
                          </a>
                        </div>
                      </td>
                    </tr>`;
                });
                tbody.innerHTML = html;

                const paginator = document.getElementById('paginatorContainer');
                if (paginator) {
                    paginator.innerHTML = renderSuraPagination(data.totalCount, currentOrdenesPageSize, page, 'loadOrdenes');
                }
            })
            .catch(err => {
                console.error("Antigravity: Error loading orders:", err);
                if (tbody) tbody.innerHTML = '<tr><td colspan="6" class="text-center text-danger py-4">Error al cargar listado.</td></tr>';
            })
            .finally(() => {
                isOrdenesLoading = false;
            });
    };

    // ── Event Delegation ──────────────────────────────────────────────────────
    $(document).off('click', '.btn-filter-ordenes').on('click', '.btn-filter-ordenes', function() {
        const val = $(this).data('val');
        setEstadoFiltro(val);
    });

    function setEstadoFiltro(val) {
        const filterEl = document.getElementById('estadoFiltro');
        if (filterEl) filterEl.value = val;
        
        // Update active button styles
        document.querySelectorAll('.btn-filter-ordenes').forEach(b => {
             // Reset styles based on shared CSS or Bootstrap
             b.classList.remove('btn-primary', 'btn-warning', 'btn-success', 'btn-secondary');
             b.classList.add('btn-outline-secondary');
             b.style.color = '';
        });
        
        // Hardcoded matching of ids or using data-val
        const activeBtn = document.querySelector(`.btn-filter-ordenes[data-val="${val}"]`) || document.querySelector(`.btn-filter-ordenes[data-val=""]`);
        if(activeBtn) {
            activeBtn.classList.remove('btn-outline-secondary');
            const colorClass = val === '1' ? 'btn-primary' : (val === '2' ? 'btn-warning' : (val === '3' ? 'btn-success' : (val === '4' ? 'btn-secondary' : 'btn-primary')));
            activeBtn.classList.add(colorClass);
            if(val === '2') activeBtn.style.color = 'white';
        }
        loadOrdenes(1);
    }

})(window, jQuery);

(function(window, $) {
    let _facturasPageSize = 10;
    let isFacturasLoading = false;

    window.initFacturas = function(retryCount = 0) {
        console.log("Antigravity: Initializing Facturas... Attempt: " + (retryCount + 1));
        const tbody = document.getElementById('tbodyFacturas');
        
        if (!tbody && retryCount < 10) {
            setTimeout(() => window.initFacturas(retryCount + 1), 200);
            return;
        }

        if (tbody) {
            isFacturasLoading = false; // Reset lock
            loadFacturas(1);
        }
    };

    window.loadFacturas = function(page, pageSize) {
        if (isFacturasLoading) return;
        if (pageSize) _facturasPageSize = parseInt(pageSize);
        page = parseInt(page) || 1;

        const tbody = document.getElementById('tbodyFacturas');
        if (!tbody) return;
        
        isFacturasLoading = true;
        tbody.innerHTML = '<tr><td colspan="6" class="text-center py-5 text-muted"><div class="spinner-border spinner-border-sm me-2"></div>Cargando...</td></tr>';

        fetch(`/Facturas/GetPaged?page=${page}&size=${_facturasPageSize}`)
            .then(r => r.json())
            .then(data => {
                const subtitle = document.getElementById('subtitleFacturas');
                if (subtitle) {
                    subtitle.textContent = `${data.totalCount} factura${data.totalCount !== 1 ? 's' : ''} · Página ${data.currentPage} de ${data.totalPages}`;
                }

                if (!data.items || data.items.length === 0) {
                    tbody.innerHTML = '<tr><td colspan="6" class="text-center py-5 text-muted">No hay facturas generadas aún.</td></tr>';
                    const paginator = document.getElementById('paginatorFacturas');
                    if (paginator) paginator.innerHTML = '';
                    return;
                }

                tbody.innerHTML = data.items.map(f => {
                    const fecha = new Date(f.fechaEmision);
                    const fechaStr = fecha.toLocaleDateString('es-CO') + ' ' + fecha.toLocaleTimeString('es-CO', { hour: '2-digit', minute: '2-digit' });
                    
                    let tipoBadge = f.tipoFacturacion === 'Electronica' 
                        ? `<span class="badge" style="background:#fff3e0;color:#e65100;border:1px solid #e65100;font-size:11px;">🔶 DIAN</span>`
                        : `<span class="badge" style="background:#e8f5e9;color:#1b5e20;border:1px solid #388e3c;font-size:11px;">✅ Interna</span>`;
                    
                    if (f.tipoFacturacion === 'Electronica') {
                        if (f.estadoEnvio === 'PendienteEnvio') tipoBadge += ` <span class="badge bg-warning text-dark ms-1" style="font-size:10px;">Pendiente</span>`;
                        else if (f.estadoEnvio === 'Enviada') tipoBadge += ` <span class="badge bg-success ms-1" style="font-size:10px;">Enviada</span>`;
                    }

                    const ordenesBadges = (f.ordenes || []).map(o => `<span class="badge bg-secondary me-1">${escHtmlLocal(o.numeroOrden)}</span>`).join('');
                    const total = (f.total || 0).toLocaleString('es-CO', { minimumFractionDigits: 0 });

                    return `<tr>
                        <td><span class="fw-bold text-primary">${escHtmlLocal(f.numeroFactura)}</span></td>
                        <td><small class="text-muted">${fechaStr}</small></td>
                        <td>${tipoBadge}</td>
                        <td>${ordenesBadges}</td>
                        <td class="text-end fw-bold">$${total}</td>
                        <td class="text-center">
                            <a href="/Facturas/Detalle/${f.id}" class="btn btn-sm btn-outline-primary me-1">Ver</a>
                            <button type="button" onclick="descargarArchivo('/Facturas/DescargarPdf/${f.id}')" class="btn btn-sm btn-outline-danger">📄 PDF</button>
                        </td>
                    </tr>`;
                }).join('');

                const paginator = document.getElementById('paginatorFacturas');
                if (paginator) {
                    paginator.innerHTML = renderSuraPagination(data.totalCount, _facturasPageSize, data.currentPage, 'loadFacturas');
                }
            })
            .catch(err => {
                console.error("Antigravity: Error loading facturas:", err);
                if (tbody) tbody.innerHTML = '<tr><td colspan="6" class="text-center text-danger py-5">Error al cargar facturas.</td></tr>';
            })
            .finally(() => {
                isFacturasLoading = false;
            });
    };

    function escHtmlLocal(s) {
        if (!s) return '';
        return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

})(window, jQuery);

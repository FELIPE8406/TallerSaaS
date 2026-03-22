(function(window, $) {
    let currentInvPageSize = 10;
    let isInventarioLoading = false;

    window.initInventario = function(retryCount = 0) {
        console.log("Antigravity: Initializing Inventario... Attempt: " + (retryCount + 1));
        const tbody = document.getElementById('tbodyInventario');
        
        if (!tbody && retryCount < 10) {
            setTimeout(() => window.initInventario(retryCount + 1), 200);
            return;
        }

        if (tbody) {
            isInventarioLoading = false; // Reset lock for fresh SPA visit
            loadInventario(1);
        }
    };

    window.loadInventario = function(page = 1, pageSize = null) {
        if (isInventarioLoading) return;
        if (pageSize) currentInvPageSize = pageSize;
        
        const buscarEl = document.getElementById('buscarLive');
        const categoriaEl = document.getElementById('categoriaSelect');
        const buscar = buscarEl ? buscarEl.value : '';
        const categoria = categoriaEl ? categoriaEl.value : '';
        
        const tbody = document.getElementById('tbodyInventario');
        if (!tbody) return;

        isInventarioLoading = true;
        tbody.innerHTML = '<tr><td colspan="6" class="text-center py-5 text-muted"><div class="spinner-border spinner-border-sm me-2"></div>Cargando inventario...</td></tr>';
        
        let url = `/Inventario/GetPaged?page=${page}&size=${currentInvPageSize}`;
        if (buscar) url += `&buscar=${encodeURIComponent(buscar)}`;
        if (categoria) url += `&categoria=${encodeURIComponent(categoria)}`;

        fetch(url)
            .then(r => r.json())
            .then(data => {
                const subtitle = document.getElementById('subtitleConteo');
                if (subtitle) {
                    subtitle.innerHTML = `${data.totalCount} productos registrados`;
                }
                
                if (!data.items || data.items.length === 0) {
                    tbody.innerHTML = '<tr><td colspan="6" class="text-center py-5 text-muted">No se encontraron productos con estos filtros.</td></tr>';
                    const paginator = document.getElementById('paginatorContainer');
                    if (paginator) paginator.innerHTML = '';
                    return;
                }
                
                const grupos = agruparInventario(data.items);
                tbody.innerHTML = '';
                
                grupos.forEach((g, gi) => {
                    const cls   = nivelClaseInv(g.stockTotal, g.stockMinimoTotal);
                    const bw    = barWidthInv(g.stockTotal, g.stockMinimoTotal);
                    const tieneMultiples = g.items.length > 1;
                    const subId = `sub-${gi}`;
                    const primerItem = g.items[0];

                    const rowPrincipal = document.createElement('tr');
                    rowPrincipal.className = 'grupo-principal';
                    rowPrincipal.innerHTML = `
                        <td style="text-align:center;cursor:${tieneMultiples ? 'pointer' : 'default'};"
                            ${tieneMultiples ? `onclick="toggleSubInv('${subId}',this)"` : ''}>
                          ${tieneMultiples ? `<i class="bi bi-chevron-right expand-icon" style="transition:transform .2s;font-size:13px;color:var(--text-muted);"></i>` : `<i class="bi bi-dash" style="font-size:13px;color:var(--text-muted);"></i>`}
                        </td>
                        <td>
                          <div style="font-weight:600;">${escHtmlInv(g.nombre)}</div>
                          ${g.sku ? `<div style="font-size:12px;color:var(--text-muted);font-family:monospace;">${escHtmlInv(g.sku)}</div>` : ''}
                          <div style="font-size:11px;color:var(--text-muted);opacity:0.8;">
                             <i class="bi bi-building me-1"></i>${tieneMultiples ? 'Multi-bodega' : escHtmlInv(primerItem.bodegaNombre || 'Sin bodega')}
                          </div>
                        </td>
                        <td class="d-none d-lg-table-cell">
                          ${g.categoria ? `<span class="badge bg-secondary">${escHtmlInv(g.categoria)}</span>` : ''}
                        </td>
                        <td>
                          <div class="d-flex align-items-center gap-2">
                            <i class="bi bi-${cls === 'success' ? 'check-circle-fill text-success' : cls === 'warning' ? 'exclamation-triangle-fill text-warning' : 'x-circle-fill text-danger'}"></i>
                            <div>
                                <strong>${g.stockTotal}</strong>
                                <div style="font-size:11px;color:var(--text-muted);">min: ${g.stockMinimoTotal}</div>
                            </div>
                            <div class="stock-bar" style="width:60px;height:6px;background:rgba(0,0,0,0.05);border-radius:3px;overflow:hidden;">
                              <div class="bg-${cls}" style="width:${bw}%;height:100%;"></div>
                            </div>
                          </div>
                        </td>
                        <td class="d-none d-lg-table-cell"><strong>$${(g.precioVenta||0).toLocaleString('es-CO')}</strong></td>
                        <td>
                          <div class="d-flex gap-1">
                            <a href="/Inventario/Editar/${primerItem.id}" class="btn btn-sm btn-outline-secondary" title="Editar"><i class="bi bi-pencil"></i></a>
                            <button type="button" class="btn btn-sm btn-outline-success" onclick="ajustarStockInv('${primerItem.id}','entrada')" title="Entrada"><i class="bi bi-plus-lg"></i></button>
                            <button type="button" class="btn btn-sm btn-outline-warning" onclick="ajustarStockInv('${primerItem.id}','salida')" title="Salida"><i class="bi bi-dash-lg"></i></button>
                          </div>
                        </td>`;
                    tbody.appendChild(rowPrincipal);

                    if (tieneMultiples) {
                        const rowSub = document.createElement('tr');
                        rowSub.id = subId;
                        rowSub.className = 'sub-bodegas';
                        rowSub.style.display = 'none';
                        const subRows = g.items.map(item => `
                            <tr>
                                <td colspan="2" style="padding-left:40px;">${escHtmlInv(item.bodegaNombre)}</td>
                                <td>${item.stock}</td>
                                <td colspan="3"></td>
                            </tr>
                        `).join('');
                        rowSub.innerHTML = `<td colspan="6" style="padding:0;"><table class="table table-sm mb-0"><tbody>${subRows}</tbody></table></td>`;
                        tbody.appendChild(rowSub);
                    }
                });
                
                const paginator = document.getElementById('paginatorContainer');
                if (paginator) {
                    paginator.innerHTML = renderSuraPagination(data.totalCount, currentInvPageSize, page, 'loadInventario');
                }
            })
            .catch(err => {
                console.error("Antigravity: Error loading inventory:", err);
                if (tbody) tbody.innerHTML = '<tr><td colspan="6" class="text-center text-danger py-5">Error al cargar listado de productos.</td></tr>';
            })
            .finally(() => {
                isInventarioLoading = false;
            });
    };

    function agruparInventario(productos) {
        const grupos = {};
        productos.forEach(p => {
            const key = p.sku ? p.sku.toUpperCase() : ('__' + (p.nombre||'').toUpperCase());
            if (!grupos[key]) {
                grupos[key] = { key, nombre: p.nombre, sku: p.sku, categoria: p.categoria, stockTotal: 0, stockMinimoTotal: 0, precioVenta: p.precioVenta, items: [] };
            }
            grupos[key].stockTotal += (p.stock || 0);
            grupos[key].stockMinimoTotal += (p.stockMinimo || 0);
            grupos[key].items.push(p);
        });
        return Object.values(grupos);
    }

    function nivelClaseInv(t, m) { return t <= 0 ? 'danger' : (t <= m ? 'warning' : 'success'); }
    function barWidthInv(s, m) { return m <= 0 ? 100 : Math.min(100, Math.round((s / m) * 100)); }
    function escHtmlInv(s) { return s ? String(s).replace(/[&<>"']/g, m => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":"&#39;"}[m])) : ''; }

    window.toggleSubInv = function(subId, iconCell) {
        const row = document.getElementById(subId);
        const icon = iconCell.querySelector('.expand-icon');
        if (!row) return;
        const open = row.style.display !== 'none';
        row.style.display = open ? 'none' : 'table-row';
        if (icon) icon.style.transform = open ? '' : 'rotate(90deg)';
    };

    window.ajustarStockInv = function(id, tipo) {
        const form = document.getElementById('formStock');
        if (!form) return;
        
        Swal.fire({
            title: tipo === 'entrada' ? 'Entrada de Stock' : 'Salida de Stock',
            input: 'number',
            inputValue: 1,
            showCancelButton: true,
            confirmButtonText: 'Confirmar'
        }).then(r => {
            if (r.isConfirmed) {
                document.getElementById('stockId').value = id;
                document.getElementById('stockTipo').value = tipo;
                document.getElementById('stockCantidad').value = r.value;
                form.submit();
            }
        });
    };

})(window, jQuery);

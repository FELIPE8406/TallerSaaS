(function (window, $) {
    let currentPageSize = 20;
    let isVehiculosLoading = false;

    window.initVehiculos = function (retryCount = 0) {
        console.log("Antigravity: Initializing Vehicles Module... Attempt: " + (retryCount + 1));
        const tbody = document.getElementById('vehiculosTableBody');
        
        // If the module container is not yet in the DOM (SPA lag), retry
        if (!tbody && retryCount < 10) {
            setTimeout(() => window.initVehiculos(retryCount + 1), 200);
            return;
        }

        if (tbody) {
            isVehiculosLoading = false; // Reset flag for fresh SPA visit
            loadVehiculos(1);
            setupSearchableSelectors();
        }
    };

    window.loadVehiculos = async function (page = 1, pageSize = null) {
        // Prevent race conditions from multiple triggers (SPA navigation during ongoing fetch)
        if (isVehiculosLoading) return;
        
        if (pageSize) currentPageSize = parseInt(pageSize);
        
        const tbody = document.getElementById('vehiculosTableBody');
        // Double check the element is still 'owned' by the current document body
        if (!tbody || !document.contains(tbody)) return;

        isVehiculosLoading = true;
        const filterEl = document.getElementById('clienteFiltroID');
        const clienteId = filterEl ? filterEl.value : '';

        // Show spinner
        tbody.innerHTML = '<tr><td colspan="5" class="text-center py-5 text-muted"><div class="spinner-border spinner-border-sm me-2"></div>Cargando...</td></tr>';
        
        try {
            let url = `/Vehiculos/GetPaged?page=${page}&size=${currentPageSize}`;
            if (clienteId) url += `&clienteId=${clienteId}`;

            const response = await fetch(url);
            if (!response.ok) throw new Error("Server response error: " + response.status);
            
            const result = await response.json();

            // Refresh total count title if exists
            const countText = document.getElementById('totalCountText');
            if (countText) countText.innerText = `${result.totalCount} vehículos registrados`;

            // Handle empty states
            if (!result.data || result.data.length === 0) {
                tbody.innerHTML = `<tr><td colspan="5" class="text-center py-5" style="color:var(--text-muted);">
                    <i class="bi bi-car-front" style="font-size:48px;opacity:.3;display:block;margin-bottom:12px;"></i>
                    No hay vehículos registrados.
                </td></tr>`;
                const paginator = document.getElementById('paginatorContainer');
                if (paginator) paginator.innerHTML = '';
                return;
            }

            // Build rows
            let rowsHtml = '';
            for (const v of result.data) {
                const colorTexto = v.color ? ` · ${v.color}` : '';
                const placaHtml = v.placa 
                    ? `<span style="font-family:monospace;font-weight:700;letter-spacing:.05em;background:var(--surface2);padding:3px 8px;border-radius:6px;border:1px solid var(--border);">${v.placa}</span>`
                    : `<span style="color:var(--text-muted);">—</span>`;
                const vinTexto = v.vin || '—';

                rowsHtml += `<tr>
                    <td>
                      <div style="display:flex;align-items:center;gap:10px;">
                        <div style="width:40px;height:40px;border-radius:8px;background:var(--surface2);border:1px solid var(--border);display:flex;align-items:center;justify-content:center;font-size:18px;color:var(--primary);">
                          <i class="bi bi-car-front"></i>
                        </div>
                        <div>
                          <div style="font-weight:600;">${v.marca} ${v.modelo}</div>
                          <div style="font-size:12px;color:var(--text-muted);">${v.anio}${colorTexto}</div>
                        </div>
                      </div>
                    </td>
                    <td class="d-none d-md-table-cell">${placaHtml}</td>
                    <td class="d-none d-md-table-cell" style="color:var(--text-secondary);">${v.clienteNombre}</td>
                    <td class="d-none d-lg-table-cell" style="font-family:monospace;font-size:12px;color:var(--text-muted);">${vinTexto}</td>
                    <td>
                      <div class="d-flex gap-1">
                        <a href="/Vehiculos/Editar/${v.id}" class="btn btn-sm btn-action-edit" title="Editar"><i class="bi bi-pencil"></i></a>
                        <a href="/Ordenes/Crear?vehiculoId=${v.id}" class="btn btn-sm btn-action-new-order" title="Nueva Orden"><i class="bi bi-file-earmark-plus"></i></a>
                        <button type="button" class="btn btn-sm btn-outline-danger btn-eliminar-vehiculo" data-id="${v.id}" data-name="${v.marca} ${v.modelo}">
                          <i class="bi bi-trash"></i>
                        </button>
                      </div>
                    </td>
                </tr>`;
            }
            tbody.innerHTML = rowsHtml;

            const paginator = document.getElementById('paginatorContainer');
            if (paginator) {
                const paginatorHtml = renderSuraPagination(result.totalCount, result.pageSize, result.pageNumber, 'loadVehiculos');
                paginator.innerHTML = paginatorHtml;
            }

        } catch (err) {
            console.error("Antigravity: Error loading vehicles:", err);
            if (tbody && document.contains(tbody)) {
                tbody.innerHTML = '<tr><td colspan="5" class="text-center py-4 text-danger">Error al cargar listado de vehículos. Por favor, reintenta.</td></tr>';
            }
        } finally {
            isVehiculosLoading = false;
        }
    };

    // ── Event Delegation ──────────────────────────────────────────────────────
    $(document).off('click', '.btn-eliminar-vehiculo').on('click', '.btn-eliminar-vehiculo', function() {
        const id = $(this).data('id');
        const name = $(this).data('name');
        confirmarEliminarVehiculo(id, name);
    });

    $(document).off('change', '#clienteFiltroID').on('change', '#clienteFiltroID', function() {
        loadVehiculos(1);
    });

    $(document).off('click', '.btn-limpiar-filtro-vehiculos').on('click', '.btn-limpiar-filtro-vehiculos', function() {
        $('#clienteFiltroID').val('').trigger('change');
    });

    // ── Confirmation ──────────────────────────────────────────────────────────
    function confirmarEliminarVehiculo(id, name) {
        Swal.fire({
            title: '¿Eliminar vehículo?',
            text: `Se eliminará el vehículo ${name}. Esta acción no se puede deshacer si tiene registros asociados.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            if (result.isConfirmed) {
                const csrfToken = $('input[name="__RequestVerificationToken"]').val();
                $.post('/Vehiculos/Eliminar', { id: id, __RequestVerificationToken: csrfToken })
                    .done(function() {
                        Swal.fire('Eliminado', 'El vehículo ha sido eliminado.', 'success');
                        loadVehiculos(1);
                    })
                    .fail(function(xhr) {
                        Swal.fire('Error', xhr.responseText || 'No se pudo eliminar el vehículo.', 'error');
                    });
            }
        });
    }

    function setupSearchableSelectors() {
        // Reserved for future global selector enhancements
    }

})(window, jQuery);

// ── Nomina Initialization (Zero-Refresh) ──
let isNominaLoading = false;

function initNomina(retryCount = 0) {
    console.log("Antigravity: Initializing Nomina... Attempt: " + (retryCount + 1));
    const tbody = document.getElementById('nomina-body');
    
    if (!tbody && retryCount < 10) {
        setTimeout(() => initNomina(retryCount + 1), 200);
        return;
    }

    if (tbody) {
        isNominaLoading = false; // Reset lock
        loadNomina(1);
    }
}

// ── Event Delegation ──
// We use .off().on() pattern to prevent duplicate listeners if the script is re-executed
$(document).off('click', '.btn-aplicar-filtros').on('click', '.btn-aplicar-filtros', function() {
    loadNomina(1);
});

$(document).off('click', '#btnGenerarNomina').on('click', '#btnGenerarNomina', function() {
    mostrarGenerarModal();
});

$(document).off('click', '.btn-ver-detalle').on('click', '.btn-ver-detalle', function() {
    verDetalle($(this).data('id'));
});

$(document).off('click', '.btn-descargar-pdf').on('click', '.btn-descargar-pdf', function() {
    window.location.href = `/Nomina/DescargarPdf/${$(this).data('id')}`;
});

$(document).off('click', '.btn-reportar-dian').on('click', '.btn-reportar-dian', function() {
    reportarDian($(this).data('id'));
});

function loadNomina(page = 1) {
    if (isNominaLoading) return;
    let period = document.getElementById('filter-period')?.value || ''; // YYYY-MM
    
    // Smart Default: If no period, use current month (e.g. for dynamic shell first-load)
    if (!period) {
        const now = new Date();
        period = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
        const input = document.getElementById('filter-period');
        if (input) input.value = period;
    }

    const status = document.getElementById('filter-status')?.value || '';
    const mechanicId = document.getElementById('filter-mechanic')?.value || '';

    const tbody = document.getElementById('nomina-body');
    if (!tbody) return;

    isNominaLoading = true;
    tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-muted"><div class="spinner-border spinner-border-sm me-2 text-primary"></div> Cargando registros de nómina…</td></tr>';

    const params = new URLSearchParams({ page, pageSize: 10, period, status, mechanicId });
    
    fetch(`/Nomina/GetPaged?${params.toString()}`)
        .then(response => response.json())
        .then(res => {
            if (res.kpis) {
                const totalNominaEl = document.getElementById('kpi-total-nomina');
                const totalComisionesEl = document.getElementById('kpi-total-comisiones');
                const pendientesDianEl = document.getElementById('kpi-pendientes-dian');
                const rentabilidadEl = document.getElementById('kpi-rentabilidad');

                if (totalNominaEl) totalNominaEl.textContent = formatCurrency(res.kpis.totalNomina);
                if (totalComisionesEl) totalComisionesEl.textContent = formatCurrency(res.kpis.totalComisiones);
                if (pendientesDianEl) pendientesDianEl.textContent = res.kpis.pendientesDIAN;
                if (rentabilidadEl) rentabilidadEl.textContent = formatCurrency(res.kpis.rentabilidadPromedio);
            }

            tbody.innerHTML = '';

            if (res.data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="6" class="text-center py-5">No se encontraron registros para este período.</td></tr>';
                return;
            }

            res.data.forEach(item => {
                const isDraft = item.statusKey === 'Draft';
                const rentabilidadIcon = item.esRentable 
                    ? '<i class="bi bi-check-circle-fill text-success" title="Rentable"></i>' 
                    : '<i class="bi bi-exclamation-triangle-fill text-warning" title="Baja Rentabilidad"></i>';

                let actions = `
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary btn-ver-detalle" data-id="${item.id}"><i class="bi bi-eye"></i></button>
                        <button class="btn btn-sm btn-outline-danger btn-descargar-pdf" data-id="${item.id}"><i class="bi bi-file-pdf"></i></button>
                `;

                if (isDraft) {
                    actions += `<button class="btn btn-sm btn-success btn-reportar-dian" data-id="${item.id}"><i class="bi bi-cloud-upload"></i></button>`;
                }
                actions += '</div>';

                const tr = document.createElement('tr');
                if (!item.esRentable) tr.classList.add('table-warning');
                tr.innerHTML = `
                    <td><strong>${item.empleado}</strong></td>
                    <td>${item.periodo}</td>
                    <td><strong>${item.totalNeto}</strong></td>
                    <td>${rentabilidadIcon} <small class="text-muted ms-1">${item.ingresosGenerados}</small></td>
                    <td><span class="badge ${item.estadoClase}">${item.estado}</span></td>
                    <td>${actions}</td>
                `;
                tbody.appendChild(tr);
            });

            renderPagination(res.total, page, 10, 'loadNomina');
        })
        .catch(err => {
            console.error("Antigravity: Error loading nomina:", err);
            if (tbody) tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-danger">Error al cargar datos.</td></tr>';
        })
        .finally(() => {
            isNominaLoading = false;
        });
}

function mostrarGenerarModal() {
    const period = document.getElementById('filter-period')?.value;
    if (!period) {
        Swal.fire('Error', 'Debe seleccionar un período (Mes/Año) antes de generar.', 'error');
        return;
    }

    Swal.fire({
        title: '¿Generar Nómina?',
        text: `Se calcularán los salarios y comisiones para el período ${period}.`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Sí, generar',
        confirmButtonColor: '#007AFF'
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({ title: 'Generando...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });
            
            const formData = new FormData();
            formData.append('periodo', period);

            fetch('/Nomina/GenerateBatch', {
                method: 'POST',
                body: formData
            })
            .then(r => r.json())
            .then(res => {
                if (res.success) {
                    Swal.fire('Éxito', 'Nómina generada correctamente.', 'success');
                    loadNomina(1);
                } else {
                    Swal.fire('Error', res.message || 'Error al generar.', 'error');
                }
            })
            .catch(() => Swal.fire('Error', 'No se pudo conectar con el servidor.', 'error'));
        }
    });
}

function reportarDian(id) {
    Swal.fire({
        title: 'Reportar a DIAN',
        text: '¿Desea enviar este registro comercialmente al sistema de nómina electrónica?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Enviar',
        confirmButtonColor: '#28a745'
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({ title: 'Enviando...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });
            
            const formData = new FormData();
            formData.append('id', id);

            fetch('/Nomina/ReportarDian', {
                method: 'POST',
                body: formData
            })
            .then(r => r.json())
            .then(res => {
                if (res.success) {
                    Swal.fire('Reportado', res.message, 'success');
                    loadNomina(1);
                } else {
                    Swal.fire('Error', res.message, 'error');
                }
            })
            .catch(() => Swal.fire('Error', 'No se pudo conectar con el servidor.', 'error'));
        }
    });
}

function verDetalle(id) {
    fetch(`/Nomina/Detalle?id=${id}`)
        .then(r => r.text())
        .then(html => {
            const modalContent = document.getElementById('modalContent');
            if(modalContent) {
                 modalContent.innerHTML = html;
                 const modal = new bootstrap.Modal(document.getElementById('modalDetalleNomina'));
                 modal.show();
            }
        })
        .catch(() => Swal.fire('Error', 'No se pudo cargar el detalle del registro.', 'error'));
}

function formatCurrency(val) {
    return new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(val);
}

function renderPagination(total, current, size, callbackName) {
    const container = document.getElementById('pagination-container');
    if(!container) return;
    
    container.innerHTML = '';
    const pages = Math.ceil(total / size);
    if (pages <= 1) { container.style.display = 'none'; return; }
    container.style.display = 'block';

    let html = '<nav><ul class="pagination pagination-sm justify-content-center">';
    for (let i = 1; i <= pages; i++) {
        html += `<li class="page-item ${i === current ? 'active' : ''}"><a class="page-link shadow-sm mx-1 rounded btn-pagination" data-page="${i}" href="#" onclick="event.preventDefault(); ${callbackName}(${i})">${i}</a></li>`;
    }
    html += '</ul></nav>';
    container.innerHTML = html;
}


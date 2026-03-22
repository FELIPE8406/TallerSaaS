(function(window, $) {
    let isPersonalLoading = false;

    window.initPersonal = function(retryCount = 0) {
        console.log("Antigravity: Initializing Personnel... Attempt: " + (retryCount + 1));
        const tbody = document.getElementById('personal-body');
        
        if (!tbody && retryCount < 10) {
            setTimeout(() => window.initPersonal(retryCount + 1), 200);
            return;
        }

        if (tbody) {
            isPersonalLoading = false; // Reset lock for fresh SPA visit
            loadPersonal();
        }
    };

    function loadPersonal() {
        if (isPersonalLoading) return;
        const tbody = document.getElementById('personal-body');
        if (!tbody) return;

        isPersonalLoading = true;
        // Show spinner explicitly as a reset
        tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4 text-muted"><div class="spinner-border spinner-border-sm me-2"></div>Cargando personal...</td></tr>';

        fetch('/EmpleadoContrato/GetList')
            .then(r => r.json())
            .then(data => {
                tbody.innerHTML = '';
                if (!data || data.length === 0) {
                    tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4">No se encontró personal.</td></tr>';
                    return;
                }

                data.forEach(emp => {
                    const salario = emp.tieneContrato ? new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(emp.salarioBase) : "-";
                    const comision = emp.tieneContrato ? emp.comision + "%" : "-";
                    const tipo = emp.tieneContrato ? emp.tipoEmpleado : "-";
                    
                    const badgeClass = emp.statusTexto === 'Activo' ? 'bg-success' : (emp.statusTexto === 'Inactivo' ? 'bg-warning' : 'bg-danger');

                    const tr = document.createElement('tr');
                    tr.innerHTML = `
                        <td><strong>${emp.name}</strong></td>
                        <td><span class="badge bg-secondary">${emp.role}</span></td>
                        <td>${tipo}</td>
                        <td>${salario}</td>
                        <td>${comision}</td>
                        <td><span class="badge ${badgeClass}">${emp.statusTexto}</span></td>
                        <td>
                            <button class="btn btn-sm btn-outline-primary btn-configurar-contrato" data-id="${emp.id}" data-name="${emp.name}">
                                <i class="fas fa-cog"></i> Configurar
                            </button>
                        </td>
                    `;
                    tbody.appendChild(tr);
                });
            })
            .catch(err => {
                console.error("Antigravity: Error loading personnel:", err);
                if (tbody) tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4 text-danger">Error al cargar datos.</td></tr>';
            })
            .finally(() => {
                isPersonalLoading = false;
            });
    }

    // ── Event Delegation ──
    $(document).off('click', '.btn-abrir-nuevo-empleado').on('click', '.btn-abrir-nuevo-empleado', function() {
        abrirModalNuevoEmpleado();
    });

    $(document).off('click', '.btn-guardar-nuevo-contrato').on('click', '.btn-guardar-nuevo-contrato', function() {
        guardarNuevoContrato();
    });

    $(document).off('click', '.btn-configurar-contrato').on('click', '.btn-configurar-contrato', function() {
        const id = $(this).data('id');
        const name = $(this).data('name');
        abrirModalContrato(id, name);
    });

    $(document).off('click', '.btn-guardar-contrato').on('click', '.btn-guardar-contrato', function() {
        guardarContrato();
    });

    function abrirModalNuevoEmpleado() {
        const form = document.getElementById('formNuevoContrato');
        if (form) form.reset();
        const modalEl = document.getElementById('modalNuevoEmpleado');
        if (modalEl) {
            const modal = new bootstrap.Modal(modalEl);
            modal.show();
        }
    }

    window.guardarNuevoContrato = function() {
        const formData = new FormData();
        const userId = document.getElementById('newEmpUserId').value;
        if (!userId) {
            Swal.fire('Atención', 'Debe seleccionar un usuario.', 'warning');
            return;
        }

        formData.append('userId', userId);
        formData.append('salarioBase', document.getElementById('newEmpSalario').value);
        formData.append('comision', document.getElementById('newEmpComision').value);
        formData.append('tipoEmpleado', document.getElementById('newEmpTipo').value);
        formData.append('urlPdf', document.getElementById('newEmpUrlPdf').value);
        formData.append('activo', 'true');

        Swal.fire({ title: 'Creando...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });
        
        fetch('/EmpleadoContrato/GuardarContrato', {
            method: 'POST',
            body: formData
        })
        .then(r => r.json())
        .then(res => {
            if (res.success) {
                Swal.fire('Éxito', 'Empleado agregado a la nómina con éxito.', 'success').then(() => {
                    AntigravityNav.navigateTo(window.location.pathname);
                    const modalEl = document.getElementById('modalNuevoEmpleado');
                    if (modalEl) bootstrap.Modal.getInstance(modalEl)?.hide();
                });
            } else {
                Swal.fire('Error', res.message, 'error');
            }
        })
        .catch(() => Swal.fire('Error', 'No se pudo guardar.', 'error'));
    };

    window.abrirModalContrato = function(id, name) {
        document.getElementById('empUserId').value = id;
        document.getElementById('modalEmpName').textContent = name;
        
        const form = document.getElementById('formContrato');
        if (form) form.reset();
        
        fetch(`/EmpleadoContrato/GetContrato?userId=${id}`)
            .then(r => r.json())
            .then(data => {
                if (data) {
                    document.getElementById('empTipo').value = data.tipoEmpleado;
                    document.getElementById('empSalario').value = data.salarioBase;
                    document.getElementById('empComision').value = data.porcentajeComision;
                    document.getElementById('empUrlPdf').value = data.urlPdf || '';
                    document.getElementById('empActivo').checked = data.activo;
                } else {
                    document.getElementById('empSalario').value = 1300000;
                    document.getElementById('empComision').value = 10;
                    document.getElementById('empActivo').checked = true;
                }
                const modalEl = document.getElementById('modalConfigurarContrato');
                if (modalEl) {
                    const modal = new bootstrap.Modal(modalEl);
                    modal.show();
                }
            });
    };

    window.guardarContrato = function() {
        const formData = new FormData();
        formData.append('userId', document.getElementById('empUserId').value);
        formData.append('salarioBase', document.getElementById('empSalario').value);
        formData.append('comision', document.getElementById('empComision').value);
        formData.append('tipoEmpleado', document.getElementById('empTipo').value);
        formData.append('urlPdf', document.getElementById('empUrlPdf').value);
        formData.append('activo', document.getElementById('empActivo').checked);

        Swal.fire({ title: 'Guardando...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });
        
        fetch('/EmpleadoContrato/GuardarContrato', {
            method: 'POST',
            body: formData
        })
        .then(r => r.json())
        .then(res => {
            if (res.success) {
                Swal.fire('Éxito', res.message, 'success').then(() => {
                    AntigravityNav.navigateTo(window.location.pathname);
                    const modalEl = document.getElementById('modalConfigurarContrato');
                    if (modalEl) bootstrap.Modal.getInstance(modalEl)?.hide();
                });
            } else {
                Swal.fire('Error', res.message, 'error');
            }
        })
        .catch(() => Swal.fire('Error', 'No se pudo guardar el contrato.', 'error'));
    };

})(window, jQuery);

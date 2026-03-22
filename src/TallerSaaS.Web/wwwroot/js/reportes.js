// ── Reports Initialization (Zero-Refresh) ──
function initReportes() {
    console.log("Reports system ready.");
    initDates();
    updateLinks();
}

// ── Event Delegation ──
$(document).off('click', '.period-btn').on('click', '.period-btn', function() {
    $('.period-btn').removeClass('active');
    $(this).addClass('active');
    updateLinks();
});

$(document).off('change', '#fechaDesde, #fechaHasta').on('change', '#fechaDesde, #fechaHasta', function() {
    updateLinks();
});

$(document).off('click', '.export-btn').on('click', '.export-btn', function(e) {
    const fmt = $(this).data('fmt');
    if (fmt === 'excel' || fmt === 'pdf') {
        e.preventDefault();
        
        const activePeriod = $('.period-btn.active').data('period') || 'trimestral';
        if (activePeriod === 'personalizado') {
            const desde = $('#fechaDesde').val();
            const hasta = $('#fechaHasta').val();
            if (!desde || !hasta) {
                Swal.fire('Atención', 'Por favor selecciona las fechas "Desde" y "Hasta" antes de exportar.', 'warning');
                return;
            }
        }

        const url = $(this).attr('data-url') || $(this).attr('href');
        if (url) window.open(url, '_blank', 'noopener,noreferrer');
    }
});

function buildUrl(tipo, fmt, period, desde, hasta) {
    const actionMap = {
        Ordenes:           '/Reportes/ExportarOrdenes',
        Facturas:          '/Reportes/ExportarFacturas',
        ClientesVehiculos: '/Reportes/ExportarClientesVehiculos'
    };
    let url = `${actionMap[tipo]}?formato=${fmt}&periodo=${period}`;
    if (period === 'personalizado') {
        if (desde) url += `&desde=${desde}`;
        if (hasta) url += `&hasta=${hasta}`;
    }
    return url;
}

function updateLinks() {
    const activePeriod = $('.period-btn.active').data('period') || 'trimestral';
    const desde = $('#fechaDesde').val() || '';
    const hasta = $('#fechaHasta').val() || '';

    $('.export-btn').each(function() {
        const tipo = $(this).data('tipo');
        const fmt = $(this).data('fmt');
        const url = buildUrl(tipo, fmt, activePeriod, desde, hasta);

        if (fmt === 'excel' || fmt === 'pdf') {
            $(this).removeAttr('href');
            $(this).attr('data-url', url);
            $(this).css('cursor', 'pointer');
        } else {
            $(this).attr('href', url);
            $(this).removeAttr('data-url');
        }
    });
}

function initDates() {
    if ($('#fechaDesde').val()) return; // Already set
    const now = new Date();
    const fmt = d => d.toISOString().slice(0, 10);
    const hace3m = fmt(new Date(now.getFullYear(), now.getMonth() - 3, now.getDate()));
    const hoy = fmt(now);
    $('#fechaDesde').val(hace3m);
    $('#fechaHasta').val(hoy);
}

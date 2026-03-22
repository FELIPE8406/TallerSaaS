(function (window, $) {

  // ── Chart instance registry — prevents memory leaks on SPA navigation ──
  let _chartIngresos  = null;
  let _chartServicios = null;

  window.initDashboardCharts = function(retryCount = 0) {
    console.log("Antigravity: Initializing Dashboard Charts... Attempt: " + (retryCount + 1));
    const ctxA = document.getElementById('chartIngresos');
    
    // Guard 1: Chart.js must be loaded
    if (typeof Chart === 'undefined' && retryCount < 10) {
        setTimeout(() => window.initDashboardCharts(retryCount + 1), 200);
        return;
    }

    // Guard 2: Canvas must be in DOM
    if (!ctxA && retryCount < 10) {
        setTimeout(() => window.initDashboardCharts(retryCount + 1), 200);
        return;
    }

    // Guard 3: window._dashData must be set (inline script may execute after dispatcher)
    if (typeof window._dashData === 'undefined' && retryCount < 15) {
        console.log("Antigravity: Waiting for _dashData...");
        setTimeout(() => window.initDashboardCharts(retryCount + 1), 150);
        return;
    }

    if (ctxA && typeof window._dashData !== 'undefined') {
        renderCharts();
    }
  };

  function renderCharts() {
    const ctxA = document.getElementById('chartIngresos');
    const ctxB = document.getElementById('chartServicios');
    if (!ctxA) return;

    if (typeof window._dashData === 'undefined') {
        console.warn("Antigravity: Dashboard data not found on window._dashData");
        return;
    }

    const { flujoCajaLabels, flujoCajaData, topServLabels, topServData } = window._dashData;

    Chart.defaults.font.family = "'Inter', -apple-system, sans-serif";
    Chart.defaults.font.size   = 12;

    const gridColor = 'rgba(0,0,0,0.04)';
    const textColor = '#6E6E73';
    const blue      = '#00d632';

    // ── DESTROY previous instances to prevent canvas-reuse errors ─────────
    // This is critical for SPA: the old Chart objects hold references to
    // canvases that were removed from the DOM. If we don't destroy them,
    // Chart.js throws "Canvas is already in use" errors.
    if (_chartIngresos) {
        _chartIngresos.destroy();
        _chartIngresos = null;
    }
    if (_chartServicios) {
        _chartServicios.destroy();
        _chartServicios = null;
    }

    // Also check Chart.js internal registry (belt-and-suspenders)
    let existingA = Chart.getChart(ctxA);
    if (existingA) existingA.destroy();

    // ── A: Flujo de Caja ────────────────────
    const gradA = ctxA.getContext('2d').createLinearGradient(0, 0, 0, 220);
    gradA.addColorStop(0,   'rgba(0,102,204,0.18)');
    gradA.addColorStop(0.6, 'rgba(0,102,204,0.04)');
    gradA.addColorStop(1,   'rgba(0,102,204,0)');

    _chartIngresos = new Chart(ctxA, {
      type: 'line',
      data: {
        labels: flujoCajaLabels,
        datasets: [{
          label: 'Ingresos COP',
          data: flujoCajaData,
          borderColor: blue,
          backgroundColor: gradA,
          fill: true,
          tension: 0.45,
          borderWidth: 2.5,
          pointBackgroundColor: '#fff',
          pointBorderColor: blue,
          pointBorderWidth: 2,
          pointRadius: 4,
          pointHoverRadius: 6,
          pointHoverBackgroundColor: blue,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#fff',
            borderColor: '#D2D2D7',
            borderWidth: 1,
            cornerRadius: 10,
            callbacks: { label: ctx => ' $ ' + Number(ctx.raw).toLocaleString('es-CO') }
          }
        },
        scales: {
          x: { grid: { color: gridColor, drawBorder: false }, ticks: { color: textColor } },
          y: {
            grid: { color: gridColor, drawBorder: false },
            ticks: {
              color: textColor,
              callback: v => '$ ' + (v >= 1000000 ? (v/1000000).toFixed(1) + 'M' : v >= 1000 ? (v/1000).toFixed(0) + 'K' : v)
            }
          }
        }
      }
    });

    // ── B: Top Servicios ───────────────────────
    if (ctxB) {
      let existingB = Chart.getChart(ctxB);
      if (existingB) existingB.destroy();

      const colors = ['#00d632','#34C759','#FF9500','#FF3B30','#6C6CFF','#5AC8FA'].slice(0, topServLabels.length);
      const gradients = colors.map(hex => {
        const g = ctxB.getContext('2d').createLinearGradient(0, 0, 300, 0);
        g.addColorStop(0, hex);
        g.addColorStop(1, hex + '60');
        return g;
      });

      _chartServicios = new Chart(ctxB, {
        type: 'bar',
        data: {
          labels: topServLabels,
          datasets: [{ label: 'Solicitudes', data: topServData, backgroundColor: gradients, borderRadius: 6, borderSkipped: false }]
        },
        options: {
          indexAxis: 'y',
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { display: false } },
          scales: {
            x: { grid: { color: gridColor, drawBorder: false }, ticks: { color: textColor, stepSize: 1 } },
            y: {
              grid: { display: false },
              ticks: { color: '#1D1D1F', font: { weight: '500' } }
            }
          }
        }
      });
    }
  }

  // ── SPA Lifecycle: Clean up charts when navigating away ─────────────────
  // AntigravityBeforeUnload fires in site.js before innerHTML replacement.
  // If we don't destroy here, the old Chart objects leak memory.
  document.addEventListener('AntigravityBeforeUnload', function() {
    if (_chartIngresos) {
        _chartIngresos.destroy();
        _chartIngresos = null;
    }
    if (_chartServicios) {
        _chartServicios.destroy();
        _chartServicios = null;
    }
    // Clear stale data so next Dashboard visit gets fresh data from server
    delete window._dashData;
  });

})(window, jQuery);

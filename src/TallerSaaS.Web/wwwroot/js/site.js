// ── SPA Navigation ────────────────────────────────────────────────────────
const AntigravityNav = {
  progressBar: null,
  
  init() {
    this.createProgressBar();
    this.bindLinks();
    window.addEventListener('popstate', () => window.location.reload());
  },

  createProgressBar() {
    this.progressBar = document.createElement('div');
    this.progressBar.id = 'nprogress';
    this.progressBar.style.cssText = 'position:fixed;top:0;left:0;width:0%;height:3px;background:#0066CC;z-index:9999;transition:width 0.3s ease;';
    document.body.appendChild(this.progressBar);
  },

  showProgress() {
    this.progressBar.style.width = '30%';
    this.progressBar.style.opacity = '1';
  },

  hideProgress() {
    this.progressBar.style.width = '100%';
    setTimeout(() => {
      this.progressBar.style.opacity = '0';
      setTimeout(() => this.progressBar.style.width = '0%', 300);
    }, 200);
  },

  bindLinks() {
    document.addEventListener('click', (e) => {
      const link = e.target.closest('.sidebar-nav .nav-item-link, .nav-spa');
      if (link && !link.getAttribute('href').startsWith('javascript') && !link.getAttribute('href').includes('Logout')) {
        const href = link.getAttribute('href');
        if (href && href !== '#') {
          e.preventDefault();
          this.navigateTo(href);
          
          // Update active state in sidebar
          document.querySelectorAll('.nav-item-link').forEach(l => l.classList.remove('active'));
          link.classList.add('active');
        }
      }
    });
  },

  async navigateTo(url) {
    this.showProgress();
    try {
      const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
      const html = await response.text();
      
      const mainContent = document.querySelector('.page-content');
      if (mainContent) {
        // Update Title if present in the response or from a hidden header
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');
        // If we got a full page by mistake or if we want to extract title
        const titleTag = doc.querySelector('title');
        if (titleTag) document.title = titleTag.innerText;

        mainContent.innerHTML = html;
        window.history.pushState({}, '', url);
        
        // Handle script re-execution
        // We look for scripts in the new content
        const scripts = mainContent.querySelectorAll('script');
        scripts.forEach(oldScript => {
          const newScript = document.createElement('script');
          Array.from(oldScript.attributes).forEach(attr => newScript.setAttribute(attr.name, attr.value));
          newScript.appendChild(document.createTextNode(oldScript.innerHTML));
          document.body.appendChild(newScript);
          newScript.remove();
        });

        // Dispatch custom event for page-specific initialization
        document.dispatchEvent(new Event('AntigravityPageLoaded'));

        // Close mobile sidebar if open
        const sidebar = document.getElementById('sidebar');
        if (sidebar && sidebar.classList.contains('mobile-open')) {
          sidebar.classList.remove('mobile-open');
          document.getElementById('sidebarOverlay').classList.remove('active');
        }
      }
    } catch (err) {
      console.error('SPA Navigation Error:', err);
      window.location.href = url;
    } finally {
      this.hideProgress();
    }
  }
};

// ── Initialize EVERYTHING ──────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
  AntigravityNav.init();

  const sidebar = document.getElementById('sidebar');
  if (sidebar && window.innerWidth > 768) {
    if (localStorage.getItem('sidebarCollapsed') === 'true')
      sidebar.classList.add('collapsed');
  }

  // Auto-hide flash alerts
  document.querySelectorAll('.alert-flash').forEach(el =>
    setTimeout(() => el.remove(), 4000)
  );
});

// Sidebar toggle
function toggleSidebar() {
  const sidebar = document.getElementById('sidebar');
  const overlay = document.getElementById('sidebarOverlay');
  if (window.innerWidth <= 768) {
    sidebar.classList.toggle('mobile-open');
    overlay.classList.toggle('active');
  } else {
    sidebar.classList.toggle('collapsed');
    localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
  }
}

// ── SweetAlert2 ─────────────────────────────────────────────────────────────
const Toast = typeof Swal !== 'undefined' ? Swal.mixin({
  toast: true, position: 'top-end', showConfirmButton: false,
  timer: 3000, timerProgressBar: true,
  didOpen: (toast) => {
    toast.addEventListener('mouseenter', Swal.stopTimer);
    toast.addEventListener('mouseleave', Swal.resumeTimer);
  }
}) : null;

function confirmarEliminar(form, nombre) {
  Swal.fire({
    title: '¿Eliminar?',
    html: `¿Deseas eliminar <strong>${nombre}</strong>? Esta acción no se puede deshacer.`,
    icon: 'warning',
    showCancelButton: true,
    confirmButtonColor: '#FF3B30',
    cancelButtonColor: '#6E6E73',
    confirmButtonText: 'Sí, eliminar',
    cancelButtonText: 'Cancelar',
    customClass: { popup: 'rounded-3' }
  }).then(result => { if (result.isConfirmed) form.submit(); });
}

function confirmarCambioEstado(form, estadoTexto) {
  Swal.fire({
    title: 'Cambiar Estado',
    html: `¿Cambiar la orden a <strong>${estadoTexto}</strong>?`,
    icon: 'question',
    showCancelButton: true,
    confirmButtonColor: '#0066CC',
    cancelButtonColor: '#6E6E73',
    confirmButtonText: 'Sí, cambiar',
    cancelButtonText: 'Cancelar',
    customClass: { popup: 'rounded-3' }
  }).then(result => { if (result.isConfirmed) form.submit(); });
}

function showSuccessToast(msg) {
  if (Toast) Toast.fire({ icon: 'success', title: msg });
}

function showErrorToast(msg) {
  if (Toast) Toast.fire({ icon: 'error', title: msg });
}

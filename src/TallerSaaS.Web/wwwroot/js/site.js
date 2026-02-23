// ── Sidebar toggle ──────────────────────────────────────────────────────────
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

// Restore sidebar state on desktop
document.addEventListener('DOMContentLoaded', function () {
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

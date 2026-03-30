/**
 * Antigravity SPA Core - Hard-Reset Reactive Architecture
 * Eliminates F5 dependency by using a Global Dispatcher and Centralized Scripts.
 */

window._agIntervals = window._agIntervals || [];

const AntigravityNav = {
    progressBar: null,

    init() {
        this.createProgressBar();
        this.setupGlobalDelegation();
        this.updateActiveNavLink();
        this.initModuleLoad(); // Run for the first page load
        window.addEventListener('popstate', () => window.location.reload());
    },

    createProgressBar() {
        this.progressBar = document.createElement('div');
        this.progressBar.id = 'ag-progress';
        this.progressBar.style.cssText = 'position:fixed;top:0;left:0;width:0%;height:3px;background:var(--primary);z-index:9999;transition:width 0.3s ease, opacity 0.3s;';
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

    updateActiveNavLink() {
        const path = window.location.pathname;
        $('.nav-item-link').removeClass('active');
        $('.nav-item-link').each(function() {
            const href = $(this).attr('href');
            if (href === path || (path && path.startsWith(href) && href !== '/')) {
                $(this).addClass('active');
            }
        });
    },

    // ── Global Dispatcher ──────────────────────────────────────────────────
    initModuleLoad(retryCount = 0) {
        const mainContent = document.querySelector('.page-content');
        if (!mainContent) return;

        console.log('Antigravity Dispatcher: Syncing module state... (attempt ' + (retryCount + 1) + ')');

        // Clear intervals to prevent memory leaks/zombies
        if (window._agIntervals) {
            window._agIntervals.forEach(clearInterval);
            window._agIntervals = [];
        }

        // Close any lingering modals or overlays
        if (typeof bootstrap !== 'undefined') {
            document.querySelectorAll('.modal.show').forEach(m => {
                bootstrap.Modal.getInstance(m)?.hide();
            });
            document.querySelectorAll('.modal-backdrop').forEach(b => b.remove());
        }
        if (typeof Swal !== 'undefined' && Swal.isVisible()) Swal.close();

        // Dispatchers mapping - Synchronized Zero-Race Architecture
        const dispatchers = [
            { selector: '#nomina-body',        fn: 'initNomina' },
            { selector: '#personal-body',      fn: 'initPersonal' },
            { selector: '#vehiculosTableBody', fn: 'initVehiculos' },
            { selector: '#tbodyOrdenes',       fn: 'initOrdenes' },
            { selector: '#tbodyFacturas',      fn: 'initFacturas' },
            { selector: '#tbodyInventario',    fn: 'initInventario' },
            { selector: '#chartIngresos',      fn: 'initDashboardCharts' },
            { selector: '#calendar',           fn: 'initAgenda' }
        ];

        let found = false;
        dispatchers.forEach(d => {
            const el = document.querySelector(d.selector);
            if (el) {
                if (typeof window[d.fn] === 'function') {
                    console.log(`Antigravity: Dispatching ${d.fn} (selector ${d.selector} found)`);
                    window[d.fn]();
                    found = true;
                } else {
                    console.warn(`Antigravity: Selector ${d.selector} found but ${d.fn} is not a function`);
                }
            }
        });

        if (!found && window.location.pathname.includes('/Agenda')) {
            if (typeof window.initAgenda === 'function') window.initAgenda();
        }

        // ── RETRY MECHANISM ──────────────────────────────────────────────────
        // If no module was found and we're not on a simple page (like Crear/Editar),
        // the DOM might not be ready yet. Retry up to 5 times with short delays.
        if (!found && retryCount < 5) {
            const delay = retryCount === 0 ? 50 : 150;
            setTimeout(() => this.initModuleLoad(retryCount + 1), delay);
            return;
        }

        // Setup searchable selectors globally
        document.querySelectorAll('[data-search-url]').forEach(setupSearchSelector);

        // Sidebar auto-close on mobile
        const sidebar = document.getElementById('sidebar');
        if (sidebar && sidebar.classList.contains('mobile-open')) {
            sidebar.classList.remove('mobile-open');
            document.getElementById('sidebarOverlay')?.classList.remove('active');
        }
    },

    // ── Navigation Logic ──────────────────────────────────────────────────
    async navigateTo(url, force = false) {
        if (!force && url === window.location.href) return;
        this.showProgress();

        try {
            const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            
            const html = await response.text();
            const mainContent = document.querySelector('.page-content');
            if (!mainContent) {
                 window.location.href = url; // Fallback
                 return;
            }

            document.dispatchEvent(new Event('AntigravityBeforeUnload'));
            
            // Update URL and Title
            window.history.pushState({}, '', url);
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            
            const title = doc.querySelector('title');
            if (title) document.title = title.innerText;

            // Extract ONLY the content inside .page-content
            const newContent = doc.querySelector('.page-content');
            const innerHtml = newContent ? newContent.innerHTML : html;
            
            // Update DOM first
            mainContent.innerHTML = innerHtml;

            // ── EXECUTE SCRIPTS FROM FETCHED PAGE ────────────────────────────────
            // Collect ALL scripts from the fetched document (view scripts + inline data scripts)
            const allScriptsInDoc = Array.from(doc.querySelectorAll('script'));
            
            // Scripts that are already part of the layout (jquery, bootstrap, site.js, etc.)
            // These should NEVER be re-loaded.
            const layoutScripts = [
                'jquery', 'bootstrap', 'sweetalert', 'chart.js', 'chart.umd',
                'fullcalendar', 'site.js', 'sura-paginator'
            ];
            
            const isLayoutScript = (src) => {
                if (!src) return false;
                const lower = src.toLowerCase();
                return layoutScripts.some(ls => lower.includes(ls));
            };

            // Separate into: external module scripts and inline scripts
            const externalModuleScripts = allScriptsInDoc.filter(s => {
                const src = s.getAttribute('src');
                return src && !isLayoutScript(src);
            });
            
            const inlineScripts = allScriptsInDoc.filter(s => !s.getAttribute('src') && s.innerHTML.trim());

            // 1. Execute inline scripts FIRST (they contain data like window._dashData)
            inlineScripts.forEach(oldScript => {
                try {
                    const newScript = document.createElement('script');
                    newScript.appendChild(document.createTextNode(oldScript.innerHTML));
                    document.body.appendChild(newScript);
                    newScript.remove();
                } catch(e) {
                    console.warn('Antigravity: Inline script execution error:', e);
                }
            });

            // 2. Load/re-register external module scripts
            //    For module scripts like ordenes.js, vehiculos.js — they define IIFEs that
            //    register window.initOrdenes etc. These only need to load ONCE.
            //    We just need to ensure they're loaded, not re-executed.
            for (const oldScript of externalModuleScripts) {
                const src = oldScript.getAttribute('src');
                // Normalize src (remove query strings for version comparison)
                const baseSrc = src.split('?')[0];
                const alreadyLoaded = Array.from(document.querySelectorAll('script[src]')).some(s => 
                    s.getAttribute('src').split('?')[0] === baseSrc
                );
                
                if (!alreadyLoaded) {
                    // First time seeing this script — load it
                    await new Promise((resolve) => {
                        const newScript = document.createElement('script');
                        newScript.src = src;
                        newScript.onload = resolve;
                        newScript.onerror = resolve;
                        document.body.appendChild(newScript);
                    });
                }
                // If already loaded, the IIFE already ran and window.init* functions exist
            }

            window.scrollTo(0, 0);
            this.updateActiveNavLink();
            
            // ── DISPATCH MODULE INIT — NO DELAY ──────────────────────────────────
            // The DOM is already set (innerHTML was assigned above).
            // The inline scripts already ran (data is set).
            // The module scripts are loaded (init functions exist on window).
            // So we can dispatch immediately — no setTimeout needed.
            this.initModuleLoad();
            document.dispatchEvent(new Event('AntigravityPageLoaded'));

        } catch (err) {
            console.error('SPA Navigation Error:', err);
            window.location.href = url;
        } finally {
            this.hideProgress();
        }
    },

    async submitForm(form) {
        if (form.enctype === 'multipart/form-data') {
            form.submit();
            return;
        }

        this.showProgress();
        try {
            const formData = new FormData(form);
            const response = await fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (response.ok) {
                const targetUrl = response.headers.get('Location') || response.url;
                await this.navigateTo(targetUrl, true);
                showSuccessToast("Guardado con éxito");
            } else if (response.status === 400) {
                const html = await response.text();
                document.querySelector('.page-content').innerHTML = html;
                this.initModuleLoad();
                showErrorToast("Verifique los datos del formulario.");
            } else {
                throw new Error(response.statusText);
            }
        } catch (err) {
            console.error('SPA Form Error:', err);
            showErrorToast("Error al procesar el formulario.");
        } finally {
            this.hideProgress();
        }
    },

    setupGlobalDelegation() {
        $(document).off('click', 'a').on('click', 'a', (e) => {
            const el = e.currentTarget;
            const href = el.getAttribute('href');
            const isNoSpa = el.classList.contains('no-spa');
            const isBtn = el.classList.contains('btn');
            
            if (href && !href.startsWith('#') && !href.startsWith('javascript') && 
                !isNoSpa && !el.target && !href.includes('Logout') && 
                (href.startsWith('/') || href.startsWith(window.location.origin))) {
                
                e.preventDefault();
                this.navigateTo(href);
            }
        });

        $(document).off('submit', 'form').on('submit', 'form', (e) => {
            const form = e.currentTarget;
            if (form.method.toLowerCase() === 'post' && !form.classList.contains('no-spa')) {
                e.preventDefault();
                this.submitForm(form);
            }
        });
    }
};

/**
 * Global Utilities
 */

async function descargarArchivo(url) {
    const toast = typeof Swal !== 'undefined' ? Swal.fire({
        title: 'Preparando descarga...',
        text: 'Por favor espera un momento.',
        allowOutsideClick: false,
        didOpen: () => Swal.showLoading()
    }) : null;

    try {
        const response = await fetch(url, {
            method: 'GET',
            credentials: 'include'
        });

        if (!response.ok) {
            throw new Error('Error en la respuesta del servidor');
        }

        const contentType = response.headers.get('Content-Type');
        if (!contentType || contentType.includes('text/html')) {
            throw new Error('El servidor devolvió un error en lugar de un archivo.');
        }

        const blob = await response.blob();
        const contentDisposition = response.headers.get('Content-Disposition');
        
        let fileName = 'archivo_geardash';
        if (contentDisposition) {
            const match = contentDisposition.match(/filename="?(.+?)"?$/);
            if (match) fileName = match[1];
        }

        const objectUrl = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = objectUrl;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();

        link.remove();
        window.URL.revokeObjectURL(objectUrl);
        
        if (toast) Swal.fire({ icon: 'success', title: 'Descarga iniciada', timer: 1500, showConfirmButton: false });

    } catch (err) {
        console.error('Error en descarga:', err);
        if (toast) {
            Swal.fire({ 
                icon: 'error', 
                title: 'Error en la descarga', 
                text: err.message || 'No se pudo completar la descarga del archivo.' 
            });
        }
    }
}

function setupSearchSelector(element) {
    if (element.dataset.initialized) return;
    element.dataset.initialized = "true";
    
    const url = element.dataset.searchUrl;
    const wrapper = $('<div class="ag-search-wrapper position-relative"></div>');
    $(element).wrap(wrapper);
    
    const dropdown = $('<div class="ag-search-dropdown shadow border rounded mt-1"></div>')
        .css({ position: 'absolute', top: '100%', left: 0, right: 0, background: 'var(--surface2)', color: 'var(--text)', border: '1px solid var(--border)', zIndex: 1000, display: 'none', maxHeight: '250px', overflowY: 'auto' });
    $(element).after(dropdown);

    let timer;
    $(element).on('input', function() {
        clearTimeout(timer);
        const q = $(this).val();
        if (q.length < 2) { dropdown.hide(); return; }

        timer = setTimeout(() => {
            $.getJSON(`${url}?q=${encodeURIComponent(q)}`, (data) => {
                dropdown.empty().show();
                if (data.length === 0) dropdown.append('<div class="p-2 text-muted">No hay resultados</div>');
                else {
                    data.forEach(item => {
                        $('<div class="p-2 border-bottom ag-search-item" style="cursor:pointer"></div>')
                            .text(item.text).data('id', item.id)
                            .appendTo(dropdown)
                            .on('click', function() {
                                $(element).val(item.text);
                                dropdown.hide();
                                const targetId = $(element).data('targetId');
                                if (targetId) {
                                    $(`#${targetId}`).val($(this).data('id')).trigger('change');
                                }
                            });
                    });
                }
            });
        }, 300);
    });

    $(document).on('click', e => { if (!$(e.target).closest('.ag-search-wrapper').length) dropdown.hide(); });
}

function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');
    if (!sidebar) return;

    if (window.innerWidth <= 768) {
        sidebar.classList.toggle('mobile-open');
        overlay?.classList.toggle('active');
    } else {
        sidebar.classList.toggle('collapsed');
        localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
    }
}

const Toast = typeof Swal !== 'undefined' ? Swal.mixin({
    toast: true, position: 'top-end', showConfirmButton: false, timer: 3000, timerProgressBar: true
}) : null;

function showSuccessToast(m) { Toast?.fire({ icon: 'success', title: m }); }
function showErrorToast(m) { Toast?.fire({ icon: 'error', title: m }); }

// ── Boot ────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    AntigravityNav.init();
    setTimeout(() => {
        $('.alert-flash').fadeOut();
    }, 4000);
});

/**
 * SuraPaginator - Generates Grupo SURA style pagination HTML for AJAX views.
 * Requires Bootstrap 5 utility classes.
 * 
 * @param {number} totalCount Total items available in the server
 * @param {number} pageSize Items per page (default: 10, 20, 50)
 * @param {number} currentPage Current active page 1-indexed
 * @param {string} clickCallbackName Global JS function name to call on page change, ex: "loadData"
 */
function renderSuraPagination(totalCount, pageSize, currentPage, clickCallbackName) {
    const totalPages = Math.ceil(totalCount / pageSize) || 1;
    if (totalPages <= 1 && totalCount === 0) return '<div class="text-center text-muted py-3">No hay registros para mostrar.</div>';

    let html = '<div class="d-flex flex-column align-items-center mt-4 mb-2">';
    
    // We remove borders to match Grupo SURA clean style and increase typography size
    html += '<ul class="pagination pagination-lg border-0 shadow-sm bg-white" style="border-radius: 8px; overflow: hidden; padding: 4px;">';

    // Previous Button
    const prevDisabled = currentPage === 1 ? 'disabled' : '';
    const prevCursor = currentPage === 1 ? 'cursor: not-allowed;' : '';
    html += `<li class="page-item ${prevDisabled}">
                <a class="page-link border-0 text-primary px-3 fw-bold bg-transparent" style="${prevCursor}" href="#" onclick="if(!this.parentElement.classList.contains('disabled')) ${clickCallbackName}(${currentPage - 1}, ${pageSize}); return false;" aria-label="Previous">
                    &lt;
                </a>
             </li>`;

    const maxVisible = 7; 
    let startPage, endPage;

    if (totalPages <= maxVisible) {
        startPage = 1;
        endPage = totalPages;
    } else {
        const half = Math.floor(maxVisible / 2);
        if (currentPage <= half + 1) {
            startPage = 1;
            endPage = maxVisible - 1; 
        } else if (currentPage >= totalPages - half) {
            startPage = totalPages - (maxVisible - 2);
            endPage = totalPages;
        } else {
            startPage = currentPage - half + 1;
            endPage = currentPage + half - 1;
        }
    }

    if (startPage > 1) {
        html += `<li class="page-item"><a class="page-link border-0 text-primary fw-medium bg-transparent" href="#" onclick="${clickCallbackName}(1, ${pageSize}); return false;">1</a></li>`;
        if (startPage > 2) {
            html += `<li class="page-item disabled"><span class="page-link border-0 text-muted bg-transparent">...</span></li>`;
        }
    }

    for (let i = startPage; i <= endPage; i++) {
        const activeClass = i === currentPage ? 'active' : '';
        const linkStyle = i === currentPage ? 'bg-primary text-white rounded-3 shadow-sm' : 'text-primary fw-medium bg-transparent';
        html += `<li class="page-item ${activeClass}"><a class="page-link border-0 ${linkStyle} mx-1" style="border-radius: 6px;" href="#" onclick="${clickCallbackName}(${i}, ${pageSize}); return false;">${i}</a></li>`;
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) {
            html += `<li class="page-item disabled"><span class="page-link border-0 text-muted bg-transparent">...</span></li>`;
        }
        html += `<li class="page-item"><a class="page-link border-0 text-primary fw-medium bg-transparent" href="#" onclick="${clickCallbackName}(${totalPages}, ${pageSize}); return false;">${totalPages}</a></li>`;
    }

    const nextDisabled = currentPage === totalPages ? 'disabled' : '';
    const nextCursor = currentPage === totalPages ? 'cursor: not-allowed;' : '';
    html += `<li class="page-item ${nextDisabled}">
                <a class="page-link border-0 text-primary px-3 fw-bold bg-transparent" style="${nextCursor}" href="#" onclick="if(!this.parentElement.classList.contains('disabled')) ${clickCallbackName}(${currentPage + 1}, ${pageSize}); return false;" aria-label="Next">
                    &gt;
                </a>
             </li>`;

    html += '</ul>';
    
    // Page Size Selector UX Improvement
    html += `
    <div class="d-flex justify-content-center text-muted small mt-3">
        <span class="me-2 align-self-center">Mostrar:</span>
        <select class="form-select form-select-sm border-0 bg-light w-auto text-primary fw-bold shadow-sm" onchange="${clickCallbackName}(1, this.value)">
            <option value="10" ${pageSize == 10 ? 'selected' : ''}>10</option>
            <option value="20" ${pageSize == 20 ? 'selected' : ''}>20</option>
            <option value="50" ${pageSize == 50 ? 'selected' : ''}>50</option>
        </select>
        <span class="ms-2 align-self-center">registros por p&aacute;gina (Total: ${totalCount})</span>
    </div>
    </div>`;

    return html;
}

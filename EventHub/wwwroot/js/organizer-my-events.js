// ==========================================
// EventHub My Events JavaScript
// ==========================================

document.addEventListener('DOMContentLoaded', function () {
    initializeMyEvents();
});

/**
 * Initialize My Events functionality
 */
function initializeMyEvents() {
    setupEventListeners();
    initializeFilters();
}

/**
 * Setup event listeners
 */
function setupEventListeners() {
    // Search functionality
    const searchInput = document.getElementById('searchEvents');
    if (searchInput) {
        searchInput.addEventListener('input', filterEvents);
    }

    // Filter dropdowns
    const statusFilter = document.getElementById('statusFilter');
    const categoryFilter = document.getElementById('categoryFilter');
    const sortFilter = document.getElementById('sortFilter');

    if (statusFilter) statusFilter.addEventListener('change', filterEvents);
    if (categoryFilter) categoryFilter.addEventListener('change', filterEvents);
    if (sortFilter) sortFilter.addEventListener('change', sortAndFilterEvents);

    // View toggle
    const gridView = document.getElementById('gridView');
    const listView = document.getElementById('listView');

    if (gridView) gridView.addEventListener('click', () => setView('grid'));
    if (listView) listView.addEventListener('click', () => setView('list'));
}

/**
 * Initialize filters
 */
function initializeFilters() {
    filterEvents();
}

/**
 * Filter events based on search and filters
 */
function filterEvents() {
    const searchTerm = document.getElementById('searchEvents')?.value.toLowerCase() || '';
    const statusFilter = document.getElementById('statusFilter')?.value || 'all';
    const categoryFilter = document.getElementById('categoryFilter')?.value || 'all';

    const eventCards = document.querySelectorAll('.event-card-container');
    let visibleCount = 0;

    eventCards.forEach(card => {
        const title = card.querySelector('.event-title')?.textContent.toLowerCase() || '';
        const venue = card.querySelector('.info-row:nth-child(3) span')?.textContent.toLowerCase() || '';
        const status = card.getAttribute('data-status') || '';
        const category = card.getAttribute('data-category') || '';

        const matchesSearch = title.includes(searchTerm) || venue.includes(searchTerm);
        const matchesStatus = statusFilter === 'all' || status === statusFilter;
        const matchesCategory = categoryFilter === 'all' || category === categoryFilter;

        if (matchesSearch && matchesStatus && matchesCategory) {
            card.style.display = '';
            visibleCount++;
        } else {
            card.style.display = 'none';
        }
    });

    // Show/hide no results message
    const noResults = document.getElementById('noResults');
    const eventsGrid = document.getElementById('eventsGrid');

    if (visibleCount === 0) {
        noResults?.classList.remove('d-none');
        eventsGrid?.classList.add('d-none');
    } else {
        noResults?.classList.add('d-none');
        eventsGrid?.classList.remove('d-none');
    }

    // Apply sorting after filtering
    sortEvents();
}

/**
 * Sort and filter events
 */
function sortAndFilterEvents() {
    filterEvents();
}

/**
 * Sort events based on selected option
 */
function sortEvents() {
    const sortFilter = document.getElementById('sortFilter');
    if (!sortFilter) return;

    const sortBy = sortFilter.value;
    const eventsGrid = document.getElementById('eventsGrid');
    if (!eventsGrid) return;

    const eventCards = Array.from(document.querySelectorAll('.event-card-container:not([style*="display: none"])'));

    eventCards.sort((a, b) => {
        switch (sortBy) {
            case 'date_desc':
                return new Date(b.getAttribute('data-date')) - new Date(a.getAttribute('data-date'));
            case 'date_asc':
                return new Date(a.getAttribute('data-date')) - new Date(b.getAttribute('data-date'));
            case 'revenue_desc':
                return parseFloat(b.getAttribute('data-revenue')) - parseFloat(a.getAttribute('data-revenue'));
            case 'tickets_desc':
                return parseInt(b.getAttribute('data-tickets')) - parseInt(a.getAttribute('data-tickets'));
            default:
                return 0;
        }
    });

    // Reappend sorted cards
    eventCards.forEach(card => {
        eventsGrid.appendChild(card);
    });
}

/**
 * Set view mode (grid or list)
 */
function setView(viewType) {
    const gridView = document.getElementById('gridView');
    const listView = document.getElementById('listView');
    const eventsGrid = document.getElementById('eventsGrid');

    if (viewType === 'grid') {
        gridView?.classList.add('active');
        listView?.classList.remove('active');
        eventsGrid?.classList.remove('list-view');
        eventsGrid?.classList.add('row', 'g-4');

        // Update column classes
        document.querySelectorAll('.event-card-container').forEach(card => {
            card.className = 'col-lg-4 col-md-6 event-card-container';
        });
    } else {
        listView?.classList.add('active');
        gridView?.classList.remove('active');
        eventsGrid?.classList.add('list-view');
        eventsGrid?.classList.remove('row', 'g-4');

        // Update column classes for list view
        document.querySelectorAll('.event-card-container').forEach(card => {
            card.className = 'event-card-container';
        });
    }
}

// Make functions available globally
window.filterEvents = filterEvents;
window.sortEvents = sortEvents;
window.setView = setView;
// ============================================
// QR SCANNER PAGE - DEBUG & DROPDOWN FIX
// ============================================

console.log('=== QR Scanner Debug Script Loaded ===');
console.log('Current Page:', window.location.pathname);
console.log('Current Time:', new Date().toISOString());

// Debug: Check if Bootstrap is available
console.log('Bootstrap Available:', typeof bootstrap !== 'undefined');
console.log('jQuery Available:', typeof jQuery !== 'undefined');

// Debug: Check navbar elements
document.addEventListener('DOMContentLoaded', function () {
    console.log('=== DOMContentLoaded Event Fired ===');

    // DEBUG: Check navbar structure
    const navbar = document.querySelector('.eventhub-navbar');
    console.log('Navbar Found:', !!navbar);

    const userDropdownToggle = document.getElementById('userDropdown');
    console.log('User Dropdown Toggle Found:', !!userDropdownToggle);

    const userDropdownMenu = document.querySelector('.user-dropdown');
    console.log('User Dropdown Menu Found:', !!userDropdownMenu);

    const navbarCollapse = document.querySelector('.navbar-collapse');
    console.log('Navbar Collapse Found:', !!navbarCollapse);

    // DEBUG: Check dropdown menu items
    if (userDropdownMenu) {
        const items = userDropdownMenu.querySelectorAll('.dropdown-item');
        console.log('Dropdown Items Count:', items.length);
        items.forEach((item, index) => {
            console.log(`  Item ${index}:`, item.textContent.trim());
        });
    }

    // DEBUG: Get user role from session/DOM
    const userName = document.getElementById('userName');
    console.log('User Name Element:', userName?.textContent);

    // ========== INITIALIZE LAYOUT DROPDOWN PROPERLY ==========

    console.log('=== Initializing Layout Dropdowns ===');

    // Re-initialize Bootstrap dropdowns
    if (typeof bootstrap !== 'undefined') {
        console.log('Re-initializing Bootstrap dropdowns...');

        // Get all dropdown toggles
        const dropdownToggles = document.querySelectorAll('[data-bs-toggle="dropdown"]');
        console.log('Found dropdown toggles:', dropdownToggles.length);

        dropdownToggles.forEach((toggle, index) => {
            console.log(`Dropdown ${index}:`, toggle.textContent.trim());

            try {
                // Create or get existing dropdown instance
                const dropdownInstance = new bootstrap.Dropdown(toggle, {
                    autoClose: true,
                    boundary: 'viewport'
                });
                console.log(`✅ Dropdown ${index} initialized`);

                // Add event listeners for debugging
                toggle.addEventListener('show.bs.dropdown', function () {
                    console.log('📂 Dropdown showing:', toggle.textContent.trim());
                });

                toggle.addEventListener('hide.bs.dropdown', function () {
                    console.log('📁 Dropdown hiding:', toggle.textContent.trim());
                });
            } catch (err) {
                console.error(`❌ Error initializing dropdown ${index}:`, err);
            }
        });
    } else {
        console.error('Bootstrap not loaded!');
    }

    // ========== USER DROPDOWN SPECIFIC INITIALIZATION ==========

    if (userDropdownToggle) {
        console.log('=== Setting up User Dropdown Specific Handlers ===');

        // Ensure user dropdown menu is properly positioned
        const parentLi = userDropdownToggle.closest('li.dropdown');
        if (parentLi) {
            console.log('Parent LI found for user dropdown');

            // Add click event for debugging
            userDropdownToggle.addEventListener('click', function (e) {
                console.log('🖱️ User dropdown clicked');
                console.log('User dropdown menu classes:', userDropdownMenu?.className);
                console.log('User dropdown menu display:', window.getComputedStyle(userDropdownMenu).display);
            });
        }
    }

    // ========== CHECK FOR QR SCANNER SPECIFIC JAVASCRIPT ==========

    console.log('=== QR Scanner Page Specific Setup ===');
    console.log('getCameraDevices function exists:', typeof getCameraDevices !== 'undefined');
    console.log('startScanner function exists:', typeof startScanner !== 'undefined');
    console.log('processTicketCode function exists:', typeof processTicketCode !== 'undefined');

    // Initialize camera (QR Scanner specific)
    if (typeof getCameraDevices === 'function') {
        console.log('Calling getCameraDevices()...');
        getCameraDevices();
        checkCameraPermissions();
    }

    // ========== FINAL CHECKS ==========

    setTimeout(() => {
        console.log('=== Final Debug Check (500ms delay) ===');

        const userDropdownMenuFinal = document.querySelector('.user-dropdown');
        if (userDropdownMenuFinal) {
            console.log('User Dropdown Menu display style:', userDropdownMenuFinal.style.display);
            console.log('User Dropdown Menu computed display:', window.getComputedStyle(userDropdownMenuFinal).display);
            console.log('User Dropdown Menu visibility:', window.getComputedStyle(userDropdownMenuFinal).visibility);
            console.log('User Dropdown Menu z-index:', window.getComputedStyle(userDropdownMenuFinal).zIndex);
            console.log('User Dropdown Menu position:', window.getComputedStyle(userDropdownMenuFinal).position);
            console.log('User Dropdown Menu classes:', userDropdownMenuFinal.className);
        }

        // Check navbar z-index
        const navbarElement = document.querySelector('.eventhub-navbar');
        if (navbarElement) {
            console.log('Navbar z-index:', window.getComputedStyle(navbarElement).zIndex);
            console.log('Navbar position:', window.getComputedStyle(navbarElement).position);
        }

        console.log('=== QR Scanner Debug Complete ===');
    }, 500);
});

// Debug: Monitor any console errors
window.addEventListener('error', function (event) {
    console.error('❌ JavaScript Error:', event.message);
    console.error('   File:', event.filename);
    console.error('   Line:', event.lineno);
});
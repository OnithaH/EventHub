// ==========================================
// EventHub Edit Event JavaScript
// (Same as Create with initialization of existing values)
// ==========================================

document.addEventListener('DOMContentLoaded', function () {
    initializeEditEventForm();
});

/**
 * Initialize Edit Event Form
 */
function initializeEditEventForm() {
    setupCharacterCounters();
    setupImageUpload();
    setupFormValidation();
    setupLivePreview();
    setMinDate();
    initializeExistingValues();
}

/**
 * Initialize existing values in preview
 */
function initializeExistingValues() {
    // Trigger preview updates with existing values
    const titleInput = document.querySelector('[name="Title"]');
    const categorySelect = document.querySelector('[name="Category"]');
    const dateInput = document.querySelector('[name="EventDate"]');
    const timeInput = document.querySelector('[name="EventTime"]');
    const venueSelect = document.querySelector('[name="VenueId"]');
    const ticketsInput = document.querySelector('[name="AvailableTickets"]');
    const priceInput = document.querySelector('[name="TicketPrice"]');

    // Update character counters
    if (titleInput) {
        const titleCount = document.getElementById('titleCount');
        if (titleCount) titleCount.textContent = titleInput.value.length;
    }

    if (document.querySelector('[name="Description"]')) {
        const descCount = document.getElementById('descriptionCount');
        const descInput = document.querySelector('[name="Description"]');
        if (descCount && descInput) descCount.textContent = descInput.value.length;
    }
}

/**
 * Setup character counters for text inputs
 */
function setupCharacterCounters() {
    // Title counter
    const titleInput = document.querySelector('[name="Title"]');
    const titleCount = document.getElementById('titleCount');

    if (titleInput && titleCount) {
        titleInput.addEventListener('input', function () {
            titleCount.textContent = this.value.length;
        });
    }

    // Description counter
    const descInput = document.querySelector('[name="Description"]');
    const descCount = document.getElementById('descriptionCount');

    if (descInput && descCount) {
        descInput.addEventListener('input', function () {
            descCount.textContent = this.value.length;
        });
    }
}

/**
 * Setup image upload functionality
 */
function setupImageUpload() {
    const uploadArea = document.getElementById('imageUploadArea');
    const fileInput = document.getElementById('eventImage');
    const placeholder = uploadArea?.querySelector('.upload-placeholder');
    const preview = document.getElementById('imagePreview');
    const previewImage = document.getElementById('previewImage');
    const removeBtn = uploadArea?.querySelector('.remove-image');

    if (!uploadArea || !fileInput) return;

    // Click to upload
    uploadArea.addEventListener('click', function (e) {
        if (e.target.closest('.remove-image')) return;
        fileInput.click();
    });

    // Drag and drop
    uploadArea.addEventListener('dragover', function (e) {
        e.preventDefault();
        this.style.borderColor = '#6366f1';
        this.style.background = '#f1f5f9';
    });

    uploadArea.addEventListener('dragleave', function (e) {
        e.preventDefault();
        this.style.borderColor = '#cbd5e1';
        this.style.background = '#f8fafc';
    });

    uploadArea.addEventListener('drop', function (e) {
        e.preventDefault();
        this.style.borderColor = '#cbd5e1';
        this.style.background = '#f8fafc';

        const files = e.dataTransfer.files;
        if (files.length > 0) {
            fileInput.files = files;
            handleImageSelect(files[0]);
        }
    });

    // File input change
    fileInput.addEventListener('change', function () {
        if (this.files && this.files[0]) {
            handleImageSelect(this.files[0]);
        }
    });

    // Remove image
    removeBtn?.addEventListener('click', function (e) {
        e.stopPropagation();
        fileInput.value = '';
        placeholder?.classList.remove('d-none');
        preview?.classList.add('d-none');
        // Restore original image in preview
        const currentImage = document.querySelector('.current-image img');
        if (currentImage) {
            updatePreviewImage(currentImage.src);
        }
    });

    function handleImageSelect(file) {
        // Validate file type
        if (!file.type.match('image.*')) {
            alert('Please select an image file');
            return;
        }

        // Validate file size (10MB)
        if (file.size > 10 * 1024 * 1024) {
            alert('Image size should be less than 10MB');
            return;
        }

        // Read and display image
        const reader = new FileReader();
        reader.onload = function (e) {
            previewImage.src = e.target.result;
            placeholder?.classList.add('d-none');
            preview?.classList.remove('d-none');
            updatePreviewImage(e.target.result);
        };
        reader.readAsDataURL(file);
    }
}

/**
 * Setup form validation
 */
function setupFormValidation() {
    const form = document.getElementById('createEventForm');

    if (!form) return;

    // Bootstrap validation
    form.addEventListener('submit', function (event) {
        if (!form.checkValidity()) {
            event.preventDefault();
            event.stopPropagation();
        }
        form.classList.add('was-validated');
    }, false);

    // Real-time validation
    const requiredFields = form.querySelectorAll('[required]');
    requiredFields.forEach(field => {
        field.addEventListener('blur', function () {
            validateField(this);
        });
    });
}

/**
 * Validate individual field
 */
function validateField(field) {
    if (!field.checkValidity()) {
        field.classList.add('is-invalid');
        field.classList.remove('is-valid');
    } else {
        field.classList.remove('is-invalid');
        field.classList.add('is-valid');
    }
}

/**
 * Setup live preview updates
 */
function setupLivePreview() {
    // Title
    const titleInput = document.querySelector('[name="Title"]');
    const previewTitle = document.getElementById('previewTitle');

    if (titleInput && previewTitle) {
        titleInput.addEventListener('input', function () {
            previewTitle.textContent = this.value || 'Event Title';
        });
    }

    // Category
    const categorySelect = document.querySelector('[name="Category"]');
    const previewCategory = document.getElementById('previewCategory');

    if (categorySelect && previewCategory) {
        categorySelect.addEventListener('change', function () {
            previewCategory.textContent = this.value || 'Category';
        });
    }

    // Date
    const dateInput = document.querySelector('[name="EventDate"]');
    const previewDate = document.getElementById('previewDate');

    if (dateInput && previewDate) {
        dateInput.addEventListener('change', function () {
            if (this.value) {
                const date = new Date(this.value);
                const options = { year: 'numeric', month: 'short', day: 'numeric' };
                previewDate.textContent = date.toLocaleDateString('en-US', options);
            } else {
                previewDate.textContent = 'Select date';
            }
        });
    }

    // Time
    const timeInput = document.querySelector('[name="EventTime"]');
    const previewTime = document.getElementById('previewTime');

    if (timeInput && previewTime) {
        timeInput.addEventListener('change', function () {
            if (this.value) {
                previewTime.textContent = formatTime(this.value);
            } else {
                previewTime.textContent = 'Select time';
            }
        });
    }

    // Venue
    const venueSelect = document.querySelector('[name="VenueId"]');
    const previewVenue = document.getElementById('previewVenue');

    if (venueSelect && previewVenue) {
        venueSelect.addEventListener('change', function () {
            const selectedOption = this.options[this.selectedIndex];
            if (selectedOption.value) {
                const venueName = selectedOption.text.split(' - ')[0];
                previewVenue.textContent = venueName;
            } else {
                previewVenue.textContent = 'Select venue';
            }
        });
    }

    // Tickets
    const ticketsInput = document.querySelector('[name="AvailableTickets"]');
    const previewTickets = document.getElementById('previewTickets');

    if (ticketsInput && previewTickets) {
        ticketsInput.addEventListener('input', function () {
            const value = parseInt(this.value) || 0;
            previewTickets.textContent = value + ' tickets';
        });
    }

    // Price
    const priceInput = document.querySelector('[name="TicketPrice"]');
    const previewPrice = document.getElementById('previewPrice');

    if (priceInput && previewPrice) {
        priceInput.addEventListener('input', function () {
            const value = parseFloat(this.value) || 0;
            previewPrice.textContent = value.toFixed(2);
        });
    }
}

/**
 * Update preview card image
 */
function updatePreviewImage(src) {
    const previewCardImage = document.getElementById('previewCardImage');
    if (previewCardImage) {
        previewCardImage.src = src;
    }
}

/**
 * Format time to 12-hour format
 */
function formatTime(time) {
    const [hours, minutes] = time.split(':');
    const hour = parseInt(hours);
    const ampm = hour >= 12 ? 'PM' : 'AM';
    const displayHour = hour % 12 || 12;
    return `${displayHour}:${minutes} ${ampm}`;
}

/**
 * Set minimum date to today
 */
function setMinDate() {
    const dateInput = document.querySelector('[name="EventDate"]');
    if (dateInput) {
        const today = new Date().toISOString().split('T')[0];
        dateInput.setAttribute('min', today);
    }
}

/**
 * Confirm before leaving page with unsaved changes
 */
let formChanged = false;
const form = document.getElementById('createEventForm');

if (form) {
    form.addEventListener('change', function () {
        formChanged = true;
    });

    window.addEventListener('beforeunload', function (e) {
        if (formChanged) {
            e.preventDefault();
            e.returnValue = '';
            return '';
        }
    });

    form.addEventListener('submit', function () {
        formChanged = false;
    });
}
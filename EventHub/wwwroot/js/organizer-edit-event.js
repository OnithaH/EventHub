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
 * ✅ NEW: Save form state on every change
 */
function saveFormStateOnChange() {
    const form = document.getElementById('createEventForm');
    if (!form) return;

    const inputs = form.querySelectorAll('input, select, textarea');
    inputs.forEach(input => {
        input.addEventListener('input', function () {
            saveCurrentFormState();
        });
        input.addEventListener('change', function () {
            saveCurrentFormState();
        });
    });
}

/**
 * ✅ NEW: Save current form state to sessionStorage
 */
function saveCurrentFormState() {
    const form = document.getElementById('createEventForm');
    if (!form) return;

    const formData = new FormData(form);
    formState = {};

    for (let [key, value] of formData.entries()) {
        formState[key] = value;
    }

    // Save preview image if exists
    const previewImg = document.getElementById('previewImage');
    if (previewImg && previewImg.src && !previewImg.src.includes('default-event.jpg')) {
        formState['preview ImageSrc'] = previewImg.src;
    }

    sessionStorage.setItem('createEventFormState', JSON.stringify(formState));
}

/**
 * ✅ NEW: Restore form state from sessionStorage
 */
function restoreFormState() {
    const savedState = sessionStorage.getItem('createEventFormState');
    if (!savedState) return;

    try {
        formState = JSON.parse(savedState);

        // Restore form fields
        Object.keys(formState).forEach(key => {
            if (key === 'previewImageSrc') return; // Handle separately

            const element = document.querySelector(`[name="${key}"]`);
            if (element) {
                element.value = formState[key];

                // Trigger preview update
                if (element.tagName === 'INPUT' || element.tagName === 'SELECT' || element.tagName === 'TEXTAREA') {
                    updatePreviewField(element);
                }
            }
        });

        // Restore preview image
        if (formState['previewImageSrc']) {
            const previewImg = document.getElementById('previewImage');
            const placeholder = document.querySelector('.upload-placeholder');
            const preview = document.getElementById('imagePreview');

            if (previewImg) {
                previewImg.src = formState['previewImageSrc'];
                placeholder?.classList.add('d-none');
                preview?.classList.remove('d-none');
                updatePreviewImage(formState['previewImageSrc']);
            }
        }

        // Update character counters
        const titleInput = document.querySelector('[name="Title"]');
        if (titleInput) {
            const titleCount = document.getElementById('titleCount');
            if (titleCount) titleCount.textContent = titleInput.value.length;
        }

        const descInput = document.querySelector('[name="Description"]');
        if (descInput) {
            const descCount = document.getElementById('descriptionCount');
            if (descCount) descCount.textContent = descInput.value.length;
        }
    } catch (error) {
        console.error('Error restoring form state:', error);
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

    uploadArea.addEventListener('click', function (e) {
        if (e.target.closest('.remove-image')) return;
        fileInput.click();
    });

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
            handleImageSelect(files[0]);
        }
    });

    fileInput.addEventListener('change', function () {
        if (this.files && this.files[0]) {
            handleImageSelect(this.files[0]);
        }
    });

    removeBtn?.addEventListener('click', function (e) {
        e.stopPropagation();
        fileInput.value = '';
        uploadedImageData = null;
        placeholder?.classList.remove('d-none');
        preview?.classList.add('d-none');
        updatePreviewImage('/images/events/default-event.jpg');
        saveCurrentFormState();
    });

    function handleImageSelect(file) {
        if (!file.type.match('image.*')) {
            alert('Please select an image file');
            return;
        }
        if (file.size > 10 * 1024 * 1024) {
            alert('Image size should be less than 10MB');
            return;
        }
        const reader = new FileReader();
        reader.onload = function (e) {
            uploadedImageData = e.target.result; // ✅ Save to global state
            previewImage.src = uploadedImageData;
            placeholder?.classList.add('d-none');
            preview?.classList.remove('d-none');
            updatePreviewImage(uploadedImageData);
            saveCurrentFormState(); // ✅ Save state with image
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

    form.addEventListener('submit', function (event) {
        if (!form.checkValidity()) {
            event.preventDefault();
            event.stopPropagation();
            saveCurrentFormState(); // Save before validation fails
        } else {
            // Clear saved state on successful submission
            sessionStorage.removeItem('createEventFormState');
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
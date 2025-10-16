// ==========================================
// EventHub Create Event JavaScript - FINAL FIX
// Compatible with jQuery Validation Unobtrusive
// ==========================================
// Toggle between existing and new venue
document.addEventListener('DOMContentLoaded', function () {
    const existingVenueRadio = document.getElementById('existingVenue');
    const newVenueRadio = document.getElementById('newVenue');
    const existingVenueSection = document.getElementById('existingVenueSection');
    const newVenueSection = document.getElementById('newVenueSection');
    const venueDropdown = document.getElementById('venueDropdown');

    function toggleVenueSections() {
        if (newVenueRadio && newVenueRadio.checked) {
            existingVenueSection.style.display = 'none';
            newVenueSection.style.display = 'block';

            // Clear and disable dropdown
            venueDropdown.value = '';
            venueDropdown.removeAttribute('required');

            // Make new venue fields required
            document.getElementById('newVenueName').setAttribute('required', 'required');
            document.getElementById('newVenueLocation').setAttribute('required', 'required');
            document.getElementById('newVenueCapacity').setAttribute('required', 'required');
        } else {
            existingVenueSection.style.display = 'block';
            newVenueSection.style.display = 'none';

            // Make dropdown required
            venueDropdown.setAttribute('required', 'required');

            // Clear and remove required from new venue fields
            document.getElementById('newVenueName').value = '';
            document.getElementById('newVenueLocation').value = '';
            document.getElementById('newVenueCapacity').value = '';
            document.getElementById('newVenueAddress').value = '';

            document.getElementById('newVenueName').removeAttribute('required');
            document.getElementById('newVenueLocation').removeAttribute('required');
            document.getElementById('newVenueCapacity').removeAttribute('required');
        }
    }

    // Add event listeners
    if (existingVenueRadio) {
        existingVenueRadio.addEventListener('change', toggleVenueSections);
    }
    if (newVenueRadio) {
        newVenueRadio.addEventListener('change', toggleVenueSections);
    }

    // Initialize on page load
    toggleVenueSections();
});


document.addEventListener('DOMContentLoaded', function () {
    initializeCreateEventForm();
});

function initializeCreateEventForm() {
    setupCharacterCounters();
    setupImageUpload();
    setupLivePreview();
    setMinDate();
    // DO NOT call setupFormValidation() - jQuery handles it
}

/**
 * Setup character counters
 */
function setupCharacterCounters() {
    const titleInput = document.querySelector('[name="Title"]');
    const titleCount = document.getElementById('titleCount');
    if (titleInput && titleCount) {
        titleInput.addEventListener('input', function () {
            titleCount.textContent = this.value.length;
        });
    }

    const descInput = document.querySelector('[name="Description"]');
    const descCount = document.getElementById('descriptionCount');
    if (descInput && descCount) {
        descInput.addEventListener('input', function () {
            descCount.textContent = this.value.length;
        });
    }
}

/**
 * Setup image upload
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
            fileInput.files = files;
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
        placeholder?.classList.remove('d-none');
        preview?.classList.add('d-none');
        updatePreviewImage('/images/events/default-event.jpg');
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
            previewImage.src = e.target.result;
            placeholder?.classList.add('d-none');
            preview?.classList.remove('d-none');
            updatePreviewImage(e.target.result);
        };
        reader.readAsDataURL(file);
    }
}

/**
 * Setup live preview
 */
function setupLivePreview() {
    const titleInput = document.querySelector('[name="Title"]');
    if (titleInput) {
        titleInput.addEventListener('input', function () {
            updatePreviewTitle(this.value);
        });
    }

    const categorySelect = document.querySelector('[name="Category"]');
    if (categorySelect) {
        categorySelect.addEventListener('change', function () {
            updatePreviewCategory(this.value);
        });
    }

    const dateInput = document.querySelector('[name="EventDate"]');
    if (dateInput) {
        dateInput.addEventListener('change', function () {
            updatePreviewDate(this.value);
        });
    }

    const timeInput = document.querySelector('[name="EventTime"]');
    if (timeInput) {
        timeInput.addEventListener('change', function () {
            updatePreviewTime(this.value);
        });
    }

    const venueSelect = document.querySelector('[name="VenueId"]');
    if (venueSelect) {
        venueSelect.addEventListener('change', function () {
            const selectedOption = this.options[this.selectedIndex];
            updatePreviewVenue(selectedOption.text);
        });
    }

    const ticketsInput = document.querySelector('[name="AvailableTickets"]');
    if (ticketsInput) {
        ticketsInput.addEventListener('input', function () {
            updatePreviewTickets(this.value);
        });
    }

    const priceInput = document.querySelector('[name="TicketPrice"]');
    if (priceInput) {
        priceInput.addEventListener('input', function () {
            updatePreviewPrice(this.value);
        });
    }
}

function updatePreviewTitle(value) {
    const previewTitle = document.getElementById('previewTitle');
    if (previewTitle) previewTitle.textContent = value || 'Event Title';
}

function updatePreviewCategory(value) {
    const previewCategory = document.getElementById('previewCategory');
    if (previewCategory) previewCategory.textContent = value || 'Category';
}

function updatePreviewDate(value) {
    const previewDate = document.getElementById('previewDate');
    if (previewDate && value) {
        const date = new Date(value);
        const options = { month: 'short', day: 'numeric', year: 'numeric' };
        previewDate.textContent = date.toLocaleDateString('en-US', options);
    }
}

function updatePreviewTime(value) {
    const previewTime = document.getElementById('previewTime');
    if (previewTime && value) previewTime.textContent = value;
}

function updatePreviewVenue(value) {
    const previewVenue = document.getElementById('previewVenue');
    if (previewVenue) previewVenue.textContent = value || 'Select venue';
}

function updatePreviewTickets(value) {
    const previewTickets = document.getElementById('previewTickets');
    if (previewTickets) {
        previewTickets.textContent = value ? `${value} tickets` : '0 tickets';
    }
}

function updatePreviewPrice(value) {
    const previewPrice = document.getElementById('previewPrice');
    if (previewPrice) {
        const price = parseFloat(value);
        previewPrice.textContent = !isNaN(price) ? price.toFixed(2) : '0.00';
    }
}

function updatePreviewImage(src) {
    const previewImage = document.getElementById('previewCardImage');
    if (previewImage) previewImage.src = src;
}

function setMinDate() {
    const dateInput = document.querySelector('[name="EventDate"]');
    if (dateInput) {
        const today = new Date().toISOString().split('T')[0];
        dateInput.setAttribute('min', today);
    }
}
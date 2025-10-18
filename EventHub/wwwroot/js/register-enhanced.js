/**
 * Enhanced Registration JavaScript for EventHub
 * Works with existing auth.js without conflicts
 */

let currentStep = 1;
let selectedRole = 'customer';

document.addEventListener('DOMContentLoaded', function () {
    initializeEnhancedRegistration();
    loadBenefits();
    setupStepNavigation();
    setupRoleSelection();
    setupInterestHandling();
    setupPhoneFormatting();
    setupDateValidation();
});

function initializeEnhancedRegistration() {
    // Check URL parameters for pre-selected role
    const urlParams = new URLSearchParams(window.location.search);
    const roleParam = urlParams.get('role');

    if (roleParam && (roleParam === 'customer' || roleParam === 'organizer')) {
        selectRole(roleParam);
    }

    // Set minimum age for date of birth (13 years ago)
    const dobInput = document.getElementById('dateOfBirth');
    if (dobInput) {
        const maxDate = new Date();
        maxDate.setFullYear(maxDate.getFullYear() - 13);
        dobInput.max = maxDate.toISOString().split('T')[0];

        // Set minimum age for a more reasonable maximum (100 years ago)
        const minDate = new Date();
        minDate.setFullYear(minDate.getFullYear() - 100);
        dobInput.min = minDate.toISOString().split('T')[0];
    }
}

function selectRole(role) {
    selectedRole = role;

    // Update role option styling
    document.querySelectorAll('.role-option').forEach(option => {
        option.classList.remove('active');
    });

    const selectedOption = document.querySelector(`[data-role="${role}"]`);
    if (selectedOption) {
        selectedOption.classList.add('active');
    }

    // Update hidden role field with correct enum value
    const roleField = document.getElementById('roleField');
    if (roleField) {
        if (role === 'customer') {
            roleField.value = '1'; // UserRole.Customer = 1
        } else if (role === 'organizer') {
            roleField.value = '2'; // UserRole.Organizer = 2
        }
    }

    // Show/hide role-specific fields
    const customerFields = document.querySelector('.customer-fields');
    const organizerFields = document.querySelector('.organizer-fields');

    if (role === 'organizer') {
        if (customerFields) customerFields.style.display = 'none';
        if (organizerFields) {
            organizerFields.style.display = 'block';
            // Make company field required for organizers
            const companyField = document.getElementById('company');
            if (companyField) {
                companyField.required = true;
            }
        }
    } else {
        if (customerFields) customerFields.style.display = 'block';
        if (organizerFields) {
            organizerFields.style.display = 'none';
            // Remove required from company field for customers
            const companyField = document.getElementById('company');
            if (companyField) {
                companyField.required = false;
            }
        }
    }

    loadBenefits();
}

function loadBenefits() {
    const benefitsList = document.getElementById('benefitsList');
    if (!benefitsList) return;

    const benefits = {
        customer: [
            { icon: 'bi-search', text: 'Discover amazing events in your city' },
            { icon: 'bi-ticket-perforated', text: 'Quick and secure ticket booking' },
            { icon: 'bi-qr-code', text: 'Digital tickets with QR codes' },
            { icon: 'bi-bell', text: 'Get notified about events you love' },
            { icon: 'bi-star', text: 'Exclusive discounts and early access' },
            { icon: 'bi-shield-check', text: 'Safe and secure transactions' }
        ],
        organizer: [
            { icon: 'bi-plus-circle', text: 'Create and manage unlimited events' },
            { icon: 'bi-graph-up', text: 'Track sales and revenue in real-time' },
            { icon: 'bi-people', text: 'Reach thousands of potential attendees' },
            { icon: 'bi-credit-card', text: 'Secure payment processing' },
            { icon: 'bi-bar-chart', text: 'Detailed analytics and insights' },
            { icon: 'bi-headset', text: 'Dedicated organizer support' }
        ]
    };

    benefitsList.innerHTML = benefits[selectedRole].map(benefit =>
        `<div class="benefit-item">
            <i class="bi ${benefit.icon}"></i>
            <span>${benefit.text}</span>
        </div>`
    ).join('');
}

function setupStepNavigation() {
    // Override the auth.js form submission to handle steps
    const form = document.getElementById('registrationForm');
    if (!form) return;

    form.addEventListener('submit', function (e) {
        if (currentStep < 3) {
            e.preventDefault();
            nextStep();
            return false;
        }

        // Let the existing auth.js handle final validation and submission
        return true;
    });
}

function nextStep() {
    if (validateCurrentStep()) {
        if (currentStep < 3) {
            currentStep++;
            updateStepDisplay();
        }
    }
}

function previousStep() {
    if (currentStep > 1) {
        currentStep--;
        updateStepDisplay();
    }
}

function validateCurrentStep() {
    const currentStepElement = document.querySelector(`[data-step="${currentStep}"].form-step.active`);
    if (!currentStepElement) return false;

    const inputs = currentStepElement.querySelectorAll('input[required], select[required]');
    let isValid = true;

    inputs.forEach(input => {
        // Use the existing auth.js validation if available
        if (window.AuthHandler && window.AuthHandler.validateField) {
            if (!window.AuthHandler.validateField(input)) {
                isValid = false;
            }
        } else {
            // Fallback validation
            if (!validateFieldBasic(input)) {
                isValid = false;
            }
        }
    });

    // Special validation for step 2
    if (currentStep === 2 && selectedRole === 'organizer') {
        const companyField = document.getElementById('company');
        if (companyField && !companyField.value.trim()) {
            showFieldError(companyField, 'Company name is required for organizers');
            isValid = false;
        }
    }

    return isValid;
}

function validateFieldBasic(field) {
    const value = field.value.trim();

    if (field.required && !value) {
        showFieldError(field, 'This field is required');
        return false;
    }

    if (field.type === 'email' && value) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(value)) {
            showFieldError(field, 'Please enter a valid email address');
            return false;
        }
    }

    clearFieldError(field);
    field.classList.add('is-valid');
    return true;
}

function showFieldError(field, message) {
    field.classList.remove('is-valid');
    field.classList.add('is-invalid');

    // Remove existing error
    const existingError = field.parentNode.querySelector('.field-validation-error');
    if (existingError) {
        existingError.remove();
    }

    // Add new error
    const errorElement = document.createElement('div');
    errorElement.className = 'field-validation-error';
    errorElement.textContent = message;
    field.parentNode.appendChild(errorElement);
}

function clearFieldError(field) {
    field.classList.remove('is-invalid', 'is-valid');
    const errorElement = field.parentNode.querySelector('.field-validation-error');
    if (errorElement) {
        errorElement.remove();
    }
}

function updateStepDisplay() {
    // Update step indicator
    document.querySelectorAll('.step').forEach((step, index) => {
        const stepNum = index + 1;
        step.classList.remove('active', 'completed');

        if (stepNum === currentStep) {
            step.classList.add('active');
        } else if (stepNum < currentStep) {
            step.classList.add('completed');
        }
    });

    // Update form steps
    document.querySelectorAll('.form-step').forEach(step => {
        step.classList.remove('active');
    });

    const activeStep = document.querySelector(`[data-step="${currentStep}"].form-step`);
    if (activeStep) {
        activeStep.classList.add('active');
    }

    // Update navigation buttons
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');
    const submitBtn = document.getElementById('submitBtn');

    if (prevBtn) prevBtn.style.display = currentStep === 1 ? 'none' : 'inline-block';
    if (nextBtn) nextBtn.style.display = currentStep === 3 ? 'none' : 'inline-block';
    if (submitBtn) submitBtn.style.display = currentStep === 3 ? 'inline-block' : 'none';

    // Update verification step content
    if (currentStep === 3) {
        updateVerificationContent();
    }
}

function updateVerificationContent() {
    const emailField = document.getElementById('email');
    const verificationEmail = document.getElementById('verificationEmail');

    if (emailField && verificationEmail) {
        verificationEmail.textContent = emailField.value;
    }
}

function setupRoleSelection() {
    // Add click handlers to role options
    document.querySelectorAll('.role-option').forEach(option => {
        option.addEventListener('click', function () {
            const role = this.getAttribute('data-role');
            selectRole(role);
        });
    });
}

function setupInterestHandling() {
    const checkboxes = document.querySelectorAll('.interest-checkbox');
    const interestsField = document.getElementById('interestsField');

    if (!interestsField) return;

    checkboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            const selectedInterests = Array.from(checkboxes)
                .filter(cb => cb.checked)
                .map(cb => cb.value);

            interestsField.value = selectedInterests.join(',');
        });
    });
}

function setupPhoneFormatting() {
    const phoneField = document.getElementById('phone');
    if (!phoneField) return;

    phoneField.addEventListener('input', function () {
        let value = this.value.replace(/\D/g, '');
        if (value.startsWith('94')) {
            value = '+' + value;
        } else if (value.startsWith('0')) {
            value = '+94' + value.substring(1);
        } else if (value.length > 0 && !value.startsWith('+')) {
            value = '+94' + value;
        }
        this.value = value;
    });
}

function setupDateValidation() {
    const dobField = document.getElementById('dateOfBirth');
    if (!dobField) return;

    dobField.addEventListener('change', function () {
        const dob = new Date(this.value);
        const age = new Date().getFullYear() - dob.getFullYear();

        if (age < 13) {
            showFieldError(this, 'You must be at least 13 years old to register');
        } else {
            clearFieldError(this);
        }
    });
}

// Utility functions for terms and privacy policy
function showTerms() {
    alert('Terms of Service\n\n1. Acceptance of Terms\nBy creating an account with EventHub, you agree to be bound by these Terms of Service.\n\n2. Account Responsibilities\nYou are responsible for maintaining the confidentiality of your account credentials.\n\n3. Event Listings and Bookings\nEvent organizers must provide accurate information. Customers acknowledge that event details may change.\n\n4. Payment and Refunds\nPayments are processed securely. Refund policies vary by event.\n\n5. Prohibited Activities\nUsers must not engage in fraud, spam, harassment, or illegal activities.');
}

function showPrivacyPolicy() {
    alert('Privacy Policy\n\nInformation We Collect\nWe collect information you provide directly, including account details and event preferences.\n\nHow We Use Information\nYour information is used to provide services, process transactions, and send notifications.\n\nInformation Sharing\nWe don\'t sell your personal information. We only share data with service providers as required.\n\nData Security\nWe implement industry-standard security measures to protect your information.\n\nYour Rights\nYou can access, update, or delete your personal information at any time.\n\nContact Us\nFor privacy questions, contact us at privacy@eventhub.lk');
}

// Enhanced form submission handling
document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('registrationForm');
    if (!form) return;

    // Override form submission to collect additional data
    form.addEventListener('submit', function (e) {
        if (currentStep !== 3) {
            e.preventDefault();
            return false;
        }

        // Collect additional form data and append to existing form
        collectAdditionalFormData();

        // Let auth.js handle the validation and submission
        return true;
    });
});

function collectAdditionalFormData() {
    const form = document.getElementById('registrationForm');
    if (!form) return;

    // Create hidden inputs for additional data
    const additionalFields = [
        { id: 'dateOfBirth', name: 'DateOfBirth' },
        { id: 'gender', name: 'Gender' },
        { id: 'city', name: 'City' },
        { id: 'interestsField', name: 'Interests' },
        { id: 'website', name: 'Website' },
        { id: 'organizationType', name: 'OrganizationType' },
        { id: 'description', name: 'Description' }
    ];

    additionalFields.forEach(field => {
        const element = document.getElementById(field.id);
        if (element && element.value) {
            // Check if hidden input already exists
            let hiddenInput = form.querySelector(`input[name="${field.name}"]`);
            if (!hiddenInput) {
                hiddenInput = document.createElement('input');
                hiddenInput.type = 'hidden';
                hiddenInput.name = field.name;
                form.appendChild(hiddenInput);
            }
            hiddenInput.value = element.value;
        }
    });

    // Handle checkboxes for preferences
    const preferences = [
        { id: 'emailNotifications', name: 'EmailNotifications' },
        { id: 'smsNotifications', name: 'SmsNotifications' },
        { id: 'marketingEmails', name: 'MarketingEmails' }
    ];

    preferences.forEach(pref => {
        const checkbox = document.getElementById(pref.id);
        if (checkbox) {
            let hiddenInput = form.querySelector(`input[name="${pref.name}"]`);
            if (!hiddenInput) {
                hiddenInput = document.createElement('input');
                hiddenInput.type = 'hidden';
                hiddenInput.name = pref.name;
                form.appendChild(hiddenInput);
            }
            hiddenInput.value = checkbox.checked ? 'true' : 'false';
        }
    });

    // Update interests field with selected checkboxes
    const checkboxes = document.querySelectorAll('.interest-checkbox:checked');
    const interestsField = document.getElementById('interestsField');
    if (interestsField) {
        const selectedInterests = Array.from(checkboxes).map(cb => cb.value);
        interestsField.value = selectedInterests.join(',');
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    // Wait for auth.js to initialize, then enhance
    setTimeout(function () {
        // Check if auth.js loaded properly
        if (typeof AuthHandler !== 'undefined') {
            console.log('Auth.js detected, enhancing registration form');
        } else {
            console.log('Auth.js not detected, using fallback validation');
        }

        // Initialize the enhanced registration regardless
        selectRole('customer'); // Default to customer
        updateStepDisplay(); // Initialize step display
    }, 100);
});
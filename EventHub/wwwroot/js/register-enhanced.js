/**
 * Enhanced Registration JavaScript for EventHub - FIXED VERSION
 */

let currentStep = 1;
let selectedRole = 'customer';

document.addEventListener('DOMContentLoaded', function () {
    console.log('🔧 Register Enhanced JS Loaded');
    initializeEnhancedRegistration();
    loadBenefits();
    setupStepNavigation();
    setupRoleSelection();
    setupInterestHandling();
    setupPhoneFormatting();
    setupDateValidation();
});

function initializeEnhancedRegistration() {
    console.log('✅ Initializing enhanced registration');
}

function selectRole(role) {
    selectedRole = role;
    console.log('🎯 Role selected:', role);
    document.getElementById('roleField').value = role === 'customer' ? '1' : '2';

    // Update UI
    document.querySelectorAll('.role-option').forEach(opt => {
        opt.classList.remove('active');
    });
    document.querySelector(`[data-role="${role}"]`)?.classList.add('active');
}

function loadBenefits() {
    console.log('📋 Loading benefits');
}

function setupStepNavigation() {
    const form = document.getElementById('registrationForm');
    if (!form) {
        console.error('❌ Registration form not found');
        return;
    }

    form.addEventListener('submit', function (e) {
        // 🔧 FIX: Prevent default and handle step navigation
        if (currentStep < 3) {
            e.preventDefault();
            nextStep();
            return false;
        }

        // On final step, let form submit
        return true;
    });

    // Setup Next buttons
    document.querySelectorAll('[data-next-step]').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            nextStep();
        });
    });

    // Setup Previous buttons
    document.querySelectorAll('[data-prev-step]').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            previousStep();
        });
    });
}

function nextStep() {
    console.log(`📍 Attempting to move from Step ${currentStep} to ${currentStep + 1}`);

    // 🔧 FIX: Clear all previous errors first
    clearAllErrors();

    if (validateCurrentStep()) {
        if (currentStep < 3) {
            currentStep++;
            updateStepDisplay();
            console.log(`✅ Moved to Step ${currentStep}`);
        }
    } else {
        console.log(`❌ Validation failed on Step ${currentStep}`);
    }
}

function previousStep() {
    console.log(`📍 Moving back from Step ${currentStep} to ${currentStep - 1}`);

    // 🔧 FIX: Clear errors when going back
    clearAllErrors();

    if (currentStep > 1) {
        currentStep--;
        updateStepDisplay();
        console.log(`✅ Moved to Step ${currentStep}`);
    }
}

// 🔧 FIX: Enhanced validation with detailed checks
function validateCurrentStep() {
    console.log(`🔍 Validating Step ${currentStep}`);
    const currentStepElement = document.querySelector(`[data-step="${currentStep}"].form-step`);

    if (!currentStepElement) {
        console.error(`❌ Step ${currentStep} element not found`);
        return false;
    }

    let isValid = true;
    const inputs = currentStepElement.querySelectorAll('input[required], select[required], textarea[required]');

    console.log(`   Found ${inputs.length} required fields`);

    inputs.forEach(input => {
        const fieldName = input.name || input.id;
        const value = input.value.trim();

        // 🔧 FIX: Actual value validation, not just DOM checking
        if (!value) {
            showFieldError(input, `${getLabelForField(fieldName)} is required`);
            console.log(`   ❌ ${fieldName} is empty`);
            isValid = false;
            return;
        }

        // 🔧 FIX: Type-specific validation
        if (input.type === 'email') {
            if (!isValidEmail(value)) {
                showFieldError(input, 'Please enter a valid email address');
                console.log(`   ❌ ${fieldName} invalid email format`);
                isValid = false;
                return;
            }
        }

        if (input.id === 'password') {
            if (value.length < 6) {
                showFieldError(input, 'Password must be at least 6 characters');
                console.log(`   ❌ Password too short`);
                isValid = false;
                return;
            }
        }

        if (input.id === 'confirmPassword') {
            const passwordField = document.getElementById('password');
            if (value !== passwordField.value) {
                showFieldError(input, 'Passwords do not match');
                console.log(`   ❌ Passwords do not match`);
                isValid = false;
                return;
            }
        }

        if (input.type === 'tel') {
            if (!isValidPhone(value)) {
                showFieldError(input, 'Please enter a valid phone number');
                console.log(`   ❌ ${fieldName} invalid phone format`);
                isValid = false;
                return;
            }
        }

        console.log(`   ✅ ${fieldName} valid`);
        clearFieldError(input);
    });

    // 🔧 FIX: Special validation for Step 2 (Organizer company)
    if (currentStep === 2 && selectedRole === 'organizer') {
        const companyField = document.getElementById('company');
        if (companyField) {
            const companyValue = companyField.value.trim();
            if (!companyValue) {
                showFieldError(companyField, 'Company name is required for organizers');
                console.log(`   ❌ Company name missing for organizer`);
                isValid = false;
            }
        }
    }

    // 🔧 FIX: Verify terms checkbox on final step
    if (currentStep === 3) {
        const termsCheckbox = document.getElementById('terms');
        if (termsCheckbox && !termsCheckbox.checked) {
            showFieldError(termsCheckbox, 'You must accept the Terms & Conditions');
            console.log(`   ❌ Terms not accepted`);
            isValid = false;
        }
    }

    return isValid;
}

// 🔧 FIX: Email validation
function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

// 🔧 FIX: Phone validation
function isValidPhone(phone) {
    // Accept various formats with +94, 0, or digits
    const phoneRegex = /^(\+94|0)?[0-9]{9,10}$/;
    return phoneRegex.test(phone.replace(/\s/g, ''));
}

// 🔧 FIX: Get label for field
function getLabelForField(fieldName) {
    const labels = {
        'name': 'Full Name',
        'email': 'Email Address',
        'password': 'Password',
        'confirmPassword': 'Confirm Password',
        'phone': 'Phone Number',
        'company': 'Company Name'
    };
    return labels[fieldName] || fieldName;
}

function validateFieldBasic(field) {
    const value = field.value.trim();
    if (!value) {
        return false;
    }

    if (field.type === 'email') {
        return isValidEmail(value);
    }

    if (field.type === 'tel') {
        return isValidPhone(value);
    }

    return true;
}

function showFieldError(field, message) {
    console.log(`🔴 Error on ${field.name || field.id}: ${message}`);

    field.classList.add('is-invalid');
    field.classList.remove('is-valid');

    let errorSpan = field.parentElement.querySelector('.field-validation-error');
    if (!errorSpan) {
        errorSpan = document.createElement('span');
        errorSpan.className = 'field-validation-error';
        field.parentElement.appendChild(errorSpan);
    }
    errorSpan.textContent = message;
    errorSpan.style.display = 'block';
}

function clearFieldError(field) {
    field.classList.remove('is-invalid');
    field.classList.add('is-valid');

    const errorSpan = field.parentElement.querySelector('.field-validation-error');
    if (errorSpan) {
        errorSpan.textContent = '';
        errorSpan.style.display = 'none';
    }
}

// 🔧 FIX: Clear ALL errors when changing steps
function clearAllErrors() {
    console.log('🧹 Clearing all validation errors');
    document.querySelectorAll('.form-input').forEach(input => {
        clearFieldError(input);
    });
    document.querySelectorAll('.field-validation-error').forEach(span => {
        span.textContent = '';
        span.style.display = 'none';
    });
}

function updateStepDisplay() {
    console.log(`📖 Updating display for Step ${currentStep}`);

    // Hide all steps
    document.querySelectorAll('.form-step').forEach(step => {
        step.classList.remove('active');
    });

    // Show current step
    const activeStep = document.querySelector(`[data-step="${currentStep}"]`);
    if (activeStep) {
        activeStep.classList.add('active');
    }

    // Update step indicator
    document.querySelectorAll('.step').forEach(indicator => {
        const stepNum = parseInt(indicator.getAttribute('data-step'));
        indicator.classList.remove('active');
        if (stepNum === currentStep) {
            indicator.classList.add('active');
        } else if (stepNum < currentStep) {
            indicator.classList.add('completed');
        }
    });

    // Update button visibility
    updateButtonVisibility();

    // Scroll to top of form
    document.querySelector('.registration-form-card').scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function updateButtonVisibility() {
    const nextBtn = document.querySelector('[data-next-step]');
    const prevBtn = document.querySelector('[data-prev-step]');
    const submitBtn = document.querySelector('button[type="submit"]');

    if (currentStep === 1) {
        if (prevBtn) prevBtn.style.display = 'none';
    } else {
        if (prevBtn) prevBtn.style.display = 'inline-block';
    }

    if (currentStep === 3) {
        if (nextBtn) nextBtn.style.display = 'none';
        if (submitBtn) submitBtn.style.display = 'inline-block';
    } else {
        if (nextBtn) nextBtn.style.display = 'inline-block';
        if (submitBtn) submitBtn.style.display = 'none';
    }
}

function updateVerificationContent() {
    const emailSpan = document.getElementById('verificationEmail');
    if (emailSpan) {
        emailSpan.textContent = document.getElementById('email').value;
    }
}

function setupRoleSelection() {
    // Already handled in selectRole()
}

function setupInterestHandling() {
    // Handle interest checkboxes if needed
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

// Initialize on DOM ready
console.log('✅ Register Enhanced JS ready');
// EventHub Authentication JavaScript
// Variables for multi-step form
let currentStep = 1;
const totalSteps = 3;

// Password toggle functionality
function togglePassword(fieldId) {
    const passwordField = document.getElementById(fieldId);
    const toggleBtn = passwordField.nextElementSibling.nextElementSibling;
    const icon = toggleBtn.querySelector('i');

    if (passwordField.type === 'password') {
        passwordField.type = 'text';
        icon.className = 'bi bi-eye-slash';
    } else {
        passwordField.type = 'password';
        icon.className = 'bi bi-eye';
    }
}

// Step navigation functions
function updateProgress() {
    const progressFill = document.querySelector('.progress-fill');
    const steps = document.querySelectorAll('.step');

    if (!progressFill) return; // Exit if not on register page

    // Update progress bar
    const progressWidth = (currentStep / totalSteps) * 100;
    progressFill.style.width = progressWidth + '%';

    // Update step indicators
    steps.forEach((step, index) => {
        const stepNumber = index + 1;
        step.classList.remove('active', 'completed');

        if (stepNumber < currentStep) {
            step.classList.add('completed');
        } else if (stepNumber === currentStep) {
            step.classList.add('active');
        }
    });
}

function showStep(stepNumber) {
    // Hide all steps
    document.querySelectorAll('.form-step').forEach(step => {
        step.classList.remove('active');
    });

    // Show current step
    const targetStep = document.querySelector(`[data-step="${stepNumber}"]`);
    if (targetStep) {
        targetStep.classList.add('active');
    }

    currentStep = stepNumber;
    updateProgress();
}

// Form validation functions
function validateCurrentStep() {
    const currentStepElement = document.querySelector(`[data-step="${currentStep}"]`);
    if (!currentStepElement) return true;

    const inputs = currentStepElement.querySelectorAll('input[required], input[type="email"]');
    let isValid = true;

    inputs.forEach(input => {
        if (!validateField(input)) {
            isValid = false;
        }
    });

    return isValid;
}

function validateField(field) {
    const feedback = field.closest('.form-group-modern').querySelector('.invalid-feedback');
    let isValid = true;
    let message = '';

    // Required field validation
    if (field.hasAttribute('required') && !field.value.trim()) {
        isValid = false;
        message = `${getFieldLabel(field)} is required`;
    }

    // Email validation
    if (field.type === 'email' && field.value.trim()) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(field.value)) {
            isValid = false;
            message = 'Please enter a valid email address';
        }
    }

    // Password validation
    if (field.id === 'Password' && field.value.trim()) {
        if (field.value.length < 6) {
            isValid = false;
            message = 'Password must be at least 6 characters long';
        }
    }

    // Confirm password validation
    if (field.id === 'ConfirmPassword' && field.value.trim()) {
        const password = document.getElementById('Password');
        if (password && field.value !== password.value) {
            isValid = false;
            message = 'Passwords do not match';
        }
    }

    // Phone validation
    if (field.type === 'tel' && field.value.trim()) {
        const phoneRegex = /^[+]?[\d\s\-\(\)]{10,}$/;
        if (!phoneRegex.test(field.value)) {
            isValid = false;
            message = 'Please enter a valid phone number';
        }
    }

    // Name validation
    if (field.id === 'Name' && field.value.trim()) {
        if (field.value.trim().length < 2) {
            isValid = false;
            message = 'Name must be at least 2 characters long';
        }
    }

    // Update field state
    if (isValid) {
        field.classList.remove('is-invalid');
        field.classList.add('is-valid');
        if (feedback) feedback.textContent = '';
    } else {
        field.classList.remove('is-valid');
        field.classList.add('is-invalid');
        if (feedback) feedback.textContent = message;
    }

    return isValid;
}

function getFieldLabel(field) {
    const label = field.parentNode.querySelector('.floating-label');
    return label ? label.textContent : field.name || field.id || 'Field';
}

// Password strength checker
function updatePasswordStrength() {
    const passwordField = document.getElementById('Password');
    const strengthFill = document.getElementById('strengthFill');
    const strengthText = document.getElementById('strengthText');

    if (!passwordField || !strengthFill || !strengthText) return;

    const password = passwordField.value;
    let strength = 0;
    let strengthLabel = '';

    // Check password criteria
    if (password.length >= 8) strength++;
    if (password.match(/[a-z]/)) strength++;
    if (password.match(/[A-Z]/)) strength++;
    if (password.match(/[0-9]/)) strength++;
    if (password.match(/[^a-zA-Z0-9]/)) strength++;

    // Remove previous strength classes
    strengthFill.className = 'strength-fill';

    switch (strength) {
        case 0:
        case 1:
            strengthFill.classList.add('weak');
            strengthLabel = 'Weak password';
            break;
        case 2:
            strengthFill.classList.add('fair');
            strengthLabel = 'Fair password';
            break;
        case 3:
        case 4:
            strengthFill.classList.add('good');
            strengthLabel = 'Good password';
            break;
        case 5:
            strengthFill.classList.add('strong');
            strengthLabel = 'Strong password';
            break;
        default:
            strengthLabel = 'Enter a password';
    }

    strengthText.textContent = strengthLabel;
}

// Role selection handling
function handleRoleSelection() {
    const roleRadios = document.querySelectorAll('input[name="Role"]');
    const organizerFields = document.querySelectorAll('.organizer-field');
    const companyField = document.getElementById('Company');

    roleRadios.forEach(radio => {
        radio.addEventListener('change', function () {
            if (this.value === 'Organizer') {
                organizerFields.forEach(field => {
                    field.style.display = 'block';
                    setTimeout(() => {
                        field.style.opacity = '1';
                        field.style.transform = 'translateY(0)';
                    }, 100);
                });
                if (companyField) companyField.required = true;
            } else {
                organizerFields.forEach(field => {
                    field.style.opacity = '0';
                    field.style.transform = 'translateY(-10px)';
                    setTimeout(() => {
                        field.style.display = 'none';
                    }, 300);
                });
                if (companyField) {
                    companyField.required = false;
                    companyField.value = '';
                }
            }
        });
    });
}

// Form submission handling
function handleFormSubmission() {
    // Login form
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', function (e) {
            const submitBtn = document.getElementById('loginBtn');
            if (submitBtn) {
                const btnText = submitBtn.querySelector('.btn-text');
                const btnLoading = submitBtn.querySelector('.btn-loading');

                if (btnText && btnLoading) {
                    btnText.classList.add('d-none');
                    btnLoading.classList.remove('d-none');
                    submitBtn.disabled = true;
                }
            }
        });
    }

    // Register form
    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        registerForm.addEventListener('submit', function (e) {
            e.preventDefault();

            // Validate all fields
            const allInputs = document.querySelectorAll('input[required]');
            let isValid = true;

            allInputs.forEach(input => {
                if (!validateField(input)) {
                    isValid = false;
                }
            });

            // Check terms agreement
            const agreeTerms = document.getElementById('AgreeTerms');
            if (agreeTerms && !agreeTerms.checked) {
                isValid = false;
                showMessage('error', 'Please agree to the Terms of Service and Privacy Policy');
                return;
            }

            if (isValid) {
                const submitBtn = document.getElementById('registerBtn');
                if (submitBtn) {
                    const btnText = submitBtn.querySelector('.btn-text');
                    const btnLoading = submitBtn.querySelector('.btn-loading');

                    if (btnText && btnLoading) {
                        btnText.classList.add('d-none');
                        btnLoading.classList.remove('d-none');
                        submitBtn.disabled = true;
                    }
                }

                // Submit the form
                this.submit();
            } else {
                showMessage('error', 'Please fix the errors above and try again');
            }
        });
    }
}

// Message handling
function showMessage(type, message) {
    // This function can be expanded to show toast notifications
    // For now, it will use browser alert as fallback
    if (type === 'error') {
        console.error(message);
        // You can implement custom toast notifications here
    } else if (type === 'success') {
        console.log(message);
        // You can implement custom toast notifications here
    }
}

// Visual feedback enhancements
function addVisualFeedback() {
    // Form field focus effects
    document.querySelectorAll('.form-control-modern').forEach(input => {
        input.addEventListener('focus', function () {
            this.parentNode.style.transform = 'scale(1.02)';
            this.parentNode.style.transition = 'transform 0.3s ease';
        });

        input.addEventListener('blur', function () {
            this.parentNode.style.transform = 'scale(1)';
        });
    });

    // Role card animations
    document.querySelectorAll('input[name="Role"]').forEach(radio => {
        radio.addEventListener('change', function () {
            document.querySelectorAll('.role-card, .role-card-expanded').forEach(card => {
                card.style.transform = 'scale(1)';
            });

            if (this.checked) {
                const card = this.nextElementSibling.querySelector('.role-card, .role-card-expanded');
                if (card) {
                    card.style.transform = 'scale(1.05)';
                    setTimeout(() => {
                        card.style.transform = 'scale(1)';
                    }, 200);
                }
            }
        });
    });
}

// Initialize animations
function initializeAnimations() {
    // Fade in animation for the auth card
    const authCard = document.querySelector('.auth-card');
    if (authCard) {
        authCard.style.opacity = '0';
        authCard.style.transform = 'translateY(30px)';
        authCard.style.transition = 'all 0.6s ease';

        setTimeout(() => {
            authCard.style.opacity = '1';
            authCard.style.transform = 'translateY(0)';
        }, 100);
    }

    // Staggered animation for form elements
    const formElements = document.querySelectorAll('.form-group-modern, .btn-auth');
    formElements.forEach((element, index) => {
        element.style.opacity = '0';
        element.style.transform = 'translateY(20px)';
        element.style.transition = 'all 0.4s ease';

        setTimeout(() => {
            element.style.opacity = '1';
            element.style.transform = 'translateY(0)';
        }, 200 + (index * 100));
    });

    // Initialize organizer fields
    const organizerFields = document.querySelectorAll('.organizer-field');
    organizerFields.forEach(field => {
        field.style.opacity = '0';
        field.style.transform = 'translateY(-10px)';
        field.style.transition = 'all 0.3s ease';
    });
}

// Event listeners setup
function setupEventListeners() {
    // Step navigation for register form
    document.querySelectorAll('.next-step').forEach(button => {
        button.addEventListener('click', function () {
            const nextStep = parseInt(this.dataset.next);

            if (validateCurrentStep()) {
                showStep(nextStep);
            }
        });
    });

    document.querySelectorAll('.prev-step').forEach(button => {
        button.addEventListener('click', function () {
            const prevStep = parseInt(this.dataset.prev);
            showStep(prevStep);
        });
    });

    // Real-time validation
    document.querySelectorAll('input').forEach(input => {
        input.addEventListener('blur', function () {
            validateField(this);
        });

        input.addEventListener('input', function () {
            if (this.classList.contains('is-invalid')) {
                validateField(this);
            }

            // Password strength checking
            if (this.id === 'Password') {
                updatePasswordStrength();
            }
        });
    });
}

// Main initialization function
function initializeAuth() {
    // Check if we're on an auth page
    if (!document.querySelector('.auth-page')) return;

    // Initialize progress if on register page
    updateProgress();

    // Setup all event listeners
    setupEventListeners();

    // Handle role selection
    handleRoleSelection();

    // Handle form submissions
    handleFormSubmission();

    // Add visual feedback
    addVisualFeedback();

    // Initialize animations
    initializeAnimations();
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    initializeAuth();
});

// Initialize when page is fully loaded (for better animation timing)
window.addEventListener('load', function () {
    // Additional initialization if needed
    console.log('EventHub Auth System Loaded');
});
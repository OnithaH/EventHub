/**
 * EventHub Authentication JavaScript
 * Compatible with .NET 8.0 and ASP.NET Core 8.0
 * Enhanced with modern ES6+ features and improved UX
 */

class AuthManager {
    constructor() {
        this.currentStep = 1;
        this.totalSteps = 3;
        this.formData = {};
        this.validationErrors = new Map();

        this.init();
    }

    init() {
        this.setupEventListeners();
        this.initializeValidation();
        this.setupPasswordStrengthIndicator();
        this.setupRoleToggle();

        // Initialize form with modern browser features
        if ('IntersectionObserver' in window) {
            this.setupScrollAnimations();
        }
    }

    setupEventListeners() {
        // Step navigation buttons
        document.addEventListener('click', (e) => {
            if (e.target.matches('.btn-next')) {
                const nextStep = parseInt(e.target.getAttribute('data-next'));
                this.validateAndProceed(nextStep);
            }

            if (e.target.matches('.btn-prev')) {
                const prevStep = parseInt(e.target.getAttribute('data-prev'));
                this.goToStep(prevStep);
            }
        });

        // Form submission
        const form = document.getElementById('registrationForm');
        if (form) {
            form.addEventListener('submit', (e) => this.handleFormSubmission(e));
        }

        // Real-time validation
        this.setupRealTimeValidation();

        // Keyboard navigation
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                const activeStep = document.querySelector('.form-step.active');
                const nextBtn = activeStep?.querySelector('.btn-next');
                if (nextBtn && !e.target.matches('textarea')) {
                    e.preventDefault();
                    nextBtn.click();
                }
            }
        });
    }

    setupRealTimeValidation() {
        // Name validation
        const nameInput = document.getElementById('nameInput');
        if (nameInput) {
            nameInput.addEventListener('blur', () => this.validateName());
            nameInput.addEventListener('input', () => this.clearFieldError('nameInput'));
        }

        // Email validation
        const emailInput = document.getElementById('emailInput');
        if (emailInput) {
            emailInput.addEventListener('blur', () => this.validateEmail());
            emailInput.addEventListener('input', () => this.clearFieldError('emailInput'));
        }

        // Phone validation
        const phoneInput = document.getElementById('phoneInput');
        if (phoneInput) {
            phoneInput.addEventListener('blur', () => this.validatePhone());
            phoneInput.addEventListener('input', (e) => {
                this.formatPhoneNumber(e);
                this.clearFieldError('phoneInput');
            });
        }

        // Password validation
        const passwordInput = document.getElementById('passwordInput');
        const confirmPasswordInput = document.getElementById('confirmPasswordInput');

        if (passwordInput) {
            passwordInput.addEventListener('input', () => {
                this.updatePasswordStrength();
                this.clearFieldError('passwordInput');
            });
            passwordInput.addEventListener('blur', () => this.validatePassword());
        }

        if (confirmPasswordInput) {
            confirmPasswordInput.addEventListener('blur', () => this.validatePasswordConfirmation());
            confirmPasswordInput.addEventListener('input', () => this.clearFieldError('confirmPasswordInput'));
        }
    }

    validateAndProceed(nextStep) {
        const currentStepValid = this.validateCurrentStep();

        if (currentStepValid) {
            this.collectCurrentStepData();
            this.goToStep(nextStep);

            if (nextStep === 3) {
                this.populateSummary();
            }
        } else {
            this.showValidationErrors();
            // Focus on first invalid field
            const firstInvalidField = document.querySelector('.form-control.is-invalid');
            if (firstInvalidField) {
                firstInvalidField.focus();
                firstInvalidField.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        }
    }

    validateCurrentStep() {
        let isValid = true;

        switch (this.currentStep) {
            case 1:
                isValid = this.validateName() && this.validateEmail() && this.validatePhone();
                break;
            case 2:
                isValid = this.validatePassword() && this.validatePasswordConfirmation() && this.validateRole();
                break;
            case 3:
                isValid = this.validateTerms();
                break;
        }

        return isValid;
    }

    validateName() {
        const nameInput = document.getElementById('nameInput');
        const name = nameInput?.value?.trim();

        if (!name) {
            this.setFieldError('nameInput', 'Name is required');
            return false;
        }

        if (name.length < 2) {
            this.setFieldError('nameInput', 'Name must be at least 2 characters long');
            return false;
        }

        if (name.length > 100) {
            this.setFieldError('nameInput', 'Name cannot exceed 100 characters');
            return false;
        }

        // Check for valid name pattern (letters, spaces, hyphens, apostrophes)
        const namePattern = /^[a-zA-Z\s\-\'\.]+$/;
        if (!namePattern.test(name)) {
            this.setFieldError('nameInput', 'Name contains invalid characters');
            return false;
        }

        this.clearFieldError('nameInput');
        return true;
    }

    validateEmail() {
        const emailInput = document.getElementById('emailInput');
        const email = emailInput?.value?.trim();

        if (!email) {
            this.setFieldError('emailInput', 'Email is required');
            return false;
        }

        // Enhanced email validation pattern (RFC 5322 compliant)
        const emailPattern = /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/;

        if (!emailPattern.test(email)) {
            this.setFieldError('emailInput', 'Please enter a valid email address');
            return false;
        }

        if (email.length > 150) {
            this.setFieldError('emailInput', 'Email cannot exceed 150 characters');
            return false;
        }

        this.clearFieldError('emailInput');
        return true;
    }

    validatePhone() {
        const phoneInput = document.getElementById('phoneInput');
        const phone = phoneInput?.value?.trim();

        if (!phone) {
            this.setFieldError('phoneInput', 'Phone number is required');
            return false;
        }

        // Remove all non-digit characters for validation
        const digitsOnly = phone.replace(/\D/g, '');

        if (digitsOnly.length < 10) {
            this.setFieldError('phoneInput', 'Phone number must be at least 10 digits');
            return false;
        }

        if (digitsOnly.length > 15) {
            this.setFieldError('phoneInput', 'Phone number cannot exceed 15 digits');
            return false;
        }

        this.clearFieldError('phoneInput');
        return true;
    }

    validatePassword() {
        const passwordInput = document.getElementById('passwordInput');
        const password = passwordInput?.value;

        if (!password) {
            this.setFieldError('passwordInput', 'Password is required');
            return false;
        }

        if (password.length < 6) {
            this.setFieldError('passwordInput', 'Password must be at least 6 characters long');
            return false;
        }

        if (password.length > 100) {
            this.setFieldError('passwordInput', 'Password cannot exceed 100 characters');
            return false;
        }

        // Check password strength
        const strength = this.calculatePasswordStrength(password);
        if (strength < 2) {
            this.setFieldError('passwordInput', 'Password is too weak. Include uppercase, lowercase, numbers, and special characters');
            return false;
        }

        this.clearFieldError('passwordInput');
        return true;
    }

    validatePasswordConfirmation() {
        const passwordInput = document.getElementById('passwordInput');
        const confirmPasswordInput = document.getElementById('confirmPasswordInput');
        const password = passwordInput?.value;
        const confirmPassword = confirmPasswordInput?.value;

        if (!confirmPassword) {
            this.setFieldError('confirmPasswordInput', 'Please confirm your password');
            return false;
        }

        if (password !== confirmPassword) {
            this.setFieldError('confirmPasswordInput', 'Passwords do not match');
            return false;
        }

        this.clearFieldError('confirmPasswordInput');
        return true;
    }

    validateRole() {
        const selectedRole = document.querySelector('input[name="Role"]:checked');

        if (!selectedRole) {
            this.setFieldError('Role', 'Please select a role');
            return false;
        }

        this.clearFieldError('Role');
        return true;
    }

    validateTerms() {
        const termsCheck = document.getElementById('termsCheck');

        if (!termsCheck?.checked) {
            this.setFieldError('termsCheck', 'Please accept the terms and conditions');
            return false;
        }

        this.clearFieldError('termsCheck');
        return true;
    }

    setFieldError(fieldId, message) {
        const field = document.getElementById(fieldId);
        if (!field) return;

        field.classList.add('is-invalid');
        field.classList.remove('is-valid');

        // Find or create error message element
        let errorElement = field.parentNode.querySelector('.invalid-feedback');
        if (!errorElement) {
            errorElement = field.nextElementSibling;
            if (!errorElement || !errorElement.classList.contains('invalid-feedback')) {
                errorElement = document.createElement('div');
                errorElement.className = 'invalid-feedback';
                field.parentNode.appendChild(errorElement);
            }
        }

        errorElement.textContent = message;
        errorElement.style.display = 'block';

        this.validationErrors.set(fieldId, message);
    }

    clearFieldError(fieldId) {
        const field = document.getElementById(fieldId);
        if (!field) return;

        field.classList.remove('is-invalid');
        field.classList.add('is-valid');

        const errorElement = field.parentNode.querySelector('.invalid-feedback');
        if (errorElement) {
            errorElement.style.display = 'none';
        }

        this.validationErrors.delete(fieldId);
    }

    goToStep(stepNumber) {
        // Hide all steps
        document.querySelectorAll('.form-step').forEach(step => {
            step.classList.remove('active');
        });

        // Show target step
        const targetStep = document.querySelector(`[data-step="${stepNumber}"]`);
        if (targetStep) {
            targetStep.classList.add('active');
        }

        // Update progress indicators
        this.updateProgressIndicator(stepNumber);

        // Update current step
        this.currentStep = stepNumber;

        // Smooth scroll to top of form
        const formCard = document.querySelector('.auth-card');
        if (formCard) {
            formCard.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }

        // Focus first input of new step
        setTimeout(() => {
            const firstInput = targetStep.querySelector('input:not([type="radio"]):not([type="checkbox"])');
            if (firstInput) {
                firstInput.focus();
            }
        }, 300);
    }

    updateProgressIndicator(activeStep) {
        document.querySelectorAll('.step').forEach((step, index) => {
            const stepNumber = index + 1;

            step.classList.remove('active', 'completed');

            if (stepNumber === activeStep) {
                step.classList.add('active');
            } else if (stepNumber < activeStep) {
                step.classList.add('completed');
            }
        });
    }

    collectCurrentStepData() {
        const currentStepElement = document.querySelector('.form-step.active');
        const inputs = currentStepElement.querySelectorAll('input, select, textarea');

        inputs.forEach(input => {
            if (input.type === 'radio') {
                if (input.checked) {
                    this.formData[input.name] = input.value;
                }
            } else if (input.type === 'checkbox') {
                this.formData[input.name] = input.checked;
            } else {
                this.formData[input.name] = input.value;
            }
        });
    }

    populateSummary() {
        // Update summary with collected data
        document.getElementById('summaryName').textContent = this.formData.Name || '-';
        document.getElementById('summaryEmail').textContent = this.formData.Email || '-';
        document.getElementById('summaryPhone').textContent = this.formData.Phone || '-';
        document.getElementById('summaryRole').textContent = this.formData.Role || '-';

        // Show/hide company summary based on role
        if (this.formData.Role === 'Organizer' && this.formData.Company) {
            document.getElementById('summaryCompany').textContent = this.formData.Company;
            document.querySelector('.company-summary').classList.remove('d-none');
        } else {
            document.querySelector('.company-summary').classList.add('d-none');
        }
    }

    setupPasswordStrengthIndicator() {
        const passwordInput = document.getElementById('passwordInput');
        if (!passwordInput) return;

        passwordInput.addEventListener('input', () => {
            this.updatePasswordStrength();
        });
    }

    updatePasswordStrength() {
        const password = document.getElementById('passwordInput')?.value || '';
        const strength = this.calculatePasswordStrength(password);
        const strengthBar = document.querySelector('.strength-bar');
        const strengthText = document.querySelector('.strength-text');

        if (!strengthBar || !strengthText) return;

        const strengthLevels = ['Very Weak', 'Weak', 'Fair', 'Good', 'Strong'];
        const strengthColors = ['#ff4757', '#ff6b7a', '#ffa502', '#2ed573', '#1dd1a1'];

        strengthBar.style.width = `${(strength / 4) * 100}%`;
        strengthBar.style.backgroundColor = strengthColors[strength];
        strengthText.textContent = `Password strength: ${strengthLevels[strength]}`;
    }

    calculatePasswordStrength(password) {
        let strength = 0;

        // Length check
        if (password.length >= 8) strength++;

        // Lowercase check
        if (/[a-z]/.test(password)) strength++;

        // Uppercase check
        if (/[A-Z]/.test(password)) strength++;

        // Number check
        if (/\d/.test(password)) strength++;

        // Special character check
        if (/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)) strength++;

        return Math.min(strength, 4);
    }

    setupRoleToggle() {
        const roleRadios = document.querySelectorAll('input[name="Role"]');
        const companyField = document.querySelector('.company-field');

        roleRadios.forEach(radio => {
            radio.addEventListener('change', (e) => {
                if (e.target.value === 'Organizer') {
                    companyField?.classList.remove('d-none');
                } else {
                    companyField?.classList.add('d-none');
                    // Clear company field when not organizer
                    const companyInput = document.getElementById('companyInput');
                    if (companyInput) companyInput.value = '';
                }
            });
        });
    }

    formatPhoneNumber(e) {
        let value = e.target.value.replace(/\D/g, '');

        // Format as (XXX) XXX-XXXX for US numbers
        if (value.length >= 6) {
            value = `(${value.slice(0, 3)}) ${value.slice(3, 6)}-${value.slice(6, 10)}`;
        } else if (value.length >= 3) {
            value = `(${value.slice(0, 3)}) ${value.slice(3)}`;
        }

        e.target.value = value;
    }

    showValidationErrors() {
        const summaryElement = document.querySelector('[asp-validation-summary]');
        if (summaryElement && this.validationErrors.size > 0) {
            summaryElement.classList.remove('d-none');

            // Create error list
            let errorHtml = '<ul class="mb-0">';
            this.validationErrors.forEach((message, field) => {
                errorHtml += `<li>${message}</li>`;
            });
            errorHtml += '</ul>';

            summaryElement.innerHTML = errorHtml;

            // Scroll to errors
            summaryElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }

    hideValidationSummary() {
        const summaryElement = document.querySelector('[asp-validation-summary]');
        if (summaryElement) {
            summaryElement.classList.add('d-none');
        }
    }

    handleFormSubmission(e) {
        e.preventDefault();

        // Final validation
        if (!this.validateCurrentStep()) {
            this.showValidationErrors();
            return;
        }

        // Show loading state
        const submitBtn = document.getElementById('submitBtn');
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Creating Account...';
        submitBtn.disabled = true;

        // Collect all form data
        this.collectCurrentStepData();

        // Submit form via AJAX for better UX
        this.submitFormAsync()
            .then(response => {
                if (response.success) {
                    this.showSuccessMessage();
                    // Redirect after short delay
                    setTimeout(() => {
                        window.location.href = response.redirectUrl || '/';
                    }, 2000);
                } else {
                    this.handleSubmissionErrors(response.errors);
                }
            })
            .catch(error => {
                console.error('Registration error:', error);
                this.showErrorMessage('An unexpected error occurred. Please try again.');
            })
            .finally(() => {
                // Restore button state
                submitBtn.innerHTML = originalText;
                submitBtn.disabled = false;
            });
    }

    async submitFormAsync() {
        const form = document.getElementById('registrationForm');
        const formData = new FormData(form);

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                }
            });

            if (response.ok) {
                const result = await response.json();
                return result;
            } else {
                const errorText = await response.text();
                throw new Error(`HTTP ${response.status}: ${errorText}`);
            }
        } catch (error) {
            throw error;
        }
    }

    handleSubmissionErrors(errors) {
        if (errors && typeof errors === 'object') {
            // Handle model validation errors
            Object.keys(errors).forEach(field => {
                if (Array.isArray(errors[field])) {
                    errors[field].forEach(error => {
                        this.setFieldError(field, error);
                    });
                } else {
                    this.setFieldError(field, errors[field]);
                }
            });

            this.showValidationErrors();

            // Go back to first step with errors
            const firstErrorStep = this.findFirstStepWithErrors();
            if (firstErrorStep) {
                this.goToStep(firstErrorStep);
            }
        }
    }

    findFirstStepWithErrors() {
        const step1Fields = ['nameInput', 'emailInput', 'phoneInput'];
        const step2Fields = ['passwordInput', 'confirmPasswordInput', 'Role'];
        const step3Fields = ['termsCheck'];

        for (const field of step1Fields) {
            if (this.validationErrors.has(field)) return 1;
        }

        for (const field of step2Fields) {
            if (this.validationErrors.has(field)) return 2;
        }

        for (const field of step3Fields) {
            if (this.validationErrors.has(field)) return 3;
        }

        return 1; // Default to first step
    }

    showSuccessMessage() {
        const successHtml = `
            <div class="alert alert-success alert-dismissible fade show" role="alert">
                <i class="bi bi-check-circle-fill me-2"></i>
                <strong>Registration Successful!</strong> Welcome to EventHub. Redirecting you now...
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;

        const form = document.getElementById('registrationForm');
        form.insertAdjacentHTML('beforebegin', successHtml);
    }

    showErrorMessage(message) {
        const errorHtml = `
            <div class="alert alert-danger alert-dismissible fade show" role="alert">
                <i class="bi bi-exclamation-triangle-fill me-2"></i>
                <strong>Registration Failed!</strong> ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;

        const form = document.getElementById('registrationForm');
        form.insertAdjacentHTML('beforebegin', errorHtml);
    }

    initializeValidation() {
        // Enhanced client-side validation using native HTML5 validation
        const form = document.getElementById('registrationForm');
        if (!form) return;

        // Disable HTML5 validation to use custom validation
        form.setAttribute('novalidate', 'true');

        // Add custom validation classes
        const inputs = form.querySelectorAll('input, select, textarea');
        inputs.forEach(input => {
            // Add Bootstrap validation classes
            input.addEventListener('invalid', (e) => {
                e.preventDefault();
                this.setFieldError(input.id, input.validationMessage);
            });

            input.addEventListener('blur', () => {
                if (input.validity.valid) {
                    this.clearFieldError(input.id);
                } else {
                    this.setFieldError(input.id, input.validationMessage);
                }
            });
        });
    }

    setupScrollAnimations() {
        // Add intersection observer for smooth animations
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                }
            });
        }, observerOptions);

        // Observe form steps
        document.querySelectorAll('.form-step').forEach(step => {
            step.style.opacity = '0';
            step.style.transform = 'translateY(20px)';
            step.style.transition = 'all 0.5s ease-in-out';
            observer.observe(step);
        });
    }

    // Public methods for external access
    static getInstance() {
        if (!this.instance) {
            this.instance = new AuthManager();
        }
        return this.instance;
    }

    // Utility method for email availability check
    async checkEmailAvailability(email) {
        try {
            const response = await fetch('/Account/CheckEmailAvailability', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: JSON.stringify({ email })
            });

            const result = await response.json();
            return result.available;
        } catch (error) {
            console.error('Error checking email availability:', error);
            return true; // Assume available if check fails
        }
    }

    // Method to pre-fill form data (useful for testing)
    prefillForm(data) {
        Object.keys(data).forEach(key => {
            const element = document.getElementById(key) || document.querySelector(`[name="${key}"]`);
            if (element) {
                if (element.type === 'radio') {
                    const radio = document.querySelector(`input[name="${key}"][value="${data[key]}"]`);
                    if (radio) radio.checked = true;
                } else {
                    element.value = data[key];
                }
            }
        });
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    AuthManager.getInstance();
});

// Export for potential module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AuthManager;
}
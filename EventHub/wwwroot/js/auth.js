/**
 * Fixed Authentication JavaScript for EventHub
 * Light theme, no yellow exclamation icons, proper password field handling
 */

class AuthHandler {
    constructor() {
        this.passwordInput = null;
        this.confirmPasswordInput = null;
        this.strengthIndicator = null;
        this.roleInputs = null;
        this.companyField = null;
        this.form = null;

        this.init();
    }

    init() {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.setupEventListeners());
        } else {
            this.setupEventListeners();
        }
    }

    setupEventListeners() {
        // Initialize form elements
        this.passwordInput = document.getElementById('password');
        this.confirmPasswordInput = document.getElementById('confirmPassword');
        this.strengthIndicator = document.querySelector('.password-strength');
        this.roleInputs = document.querySelectorAll('input[name="Role"]');
        this.companyField = document.querySelector('.company-field');
        this.form = document.querySelector('.auth-form');

        // Password strength checking (visual only)
        if (this.passwordInput) {
            this.passwordInput.addEventListener('input', (e) => {
                this.checkPasswordStrength(e.target.value);
            });
        }

        // Password confirmation matching (visual only)
        if (this.confirmPasswordInput) {
            this.confirmPasswordInput.addEventListener('input', (e) => {
                this.checkPasswordMatch();
            });
        }

        // Password toggle functionality
        this.setupPasswordToggle();

        // Role selection handling
        if (this.roleInputs.length > 0) {
            this.roleInputs.forEach(input => {
                input.addEventListener('change', () => {
                    this.handleRoleChange(input.value);
                });
            });
        }

        // Enhanced form validation (frontend only)
        if (this.form) {
            this.form.addEventListener('submit', (e) => {
                this.handleFormSubmit(e);
            });
        }

        // Real-time validation feedback
        this.setupRealTimeValidation();

        // Accessibility improvements
        this.setupAccessibility();

        // Visual enhancements
        this.setupVisualEnhancements();
    }

    checkPasswordStrength(password) {
        if (!this.strengthIndicator) return;

        const strength = this.calculatePasswordStrength(password);

        // Remove existing classes
        this.strengthIndicator.classList.remove('weak', 'fair', 'good', 'strong');

        // Add appropriate class
        if (password.length === 0) {
            this.strengthIndicator.style.opacity = '0';
            return;
        }

        this.strengthIndicator.style.opacity = '1';

        if (strength.score < 2) {
            this.strengthIndicator.classList.add('weak');
        } else if (strength.score < 3) {
            this.strengthIndicator.classList.add('fair');
        } else if (strength.score < 4) {
            this.strengthIndicator.classList.add('good');
        } else {
            this.strengthIndicator.classList.add('strong');
        }

        // Update ARIA label for screen readers
        const strengthText = ['very weak', 'weak', 'fair', 'good', 'strong'][strength.score];
        this.strengthIndicator.setAttribute('aria-label', `Password strength: ${strengthText}`);
    }

    calculatePasswordStrength(password) {
        let score = 0;

        if (password.length >= 6) score++;  // Minimum length for existing backend
        if (password.length >= 8) score++;  // Better length
        if (/[a-z]/.test(password) && /[A-Z]/.test(password)) score++;  // Mixed case
        if (/\d/.test(password)) score++;  // Numbers
        if (/[^a-zA-Z0-9]/.test(password)) score++;  // Special characters

        return { score };
    }

    checkPasswordMatch() {
        if (!this.passwordInput || !this.confirmPasswordInput) return;

        const password = this.passwordInput.value;
        const confirmPassword = this.confirmPasswordInput.value;

        if (confirmPassword.length === 0) {
            this.confirmPasswordInput.classList.remove('is-valid', 'is-invalid');
            return;
        }

        if (password === confirmPassword) {
            this.confirmPasswordInput.classList.remove('is-invalid');
            this.confirmPasswordInput.classList.add('is-valid');
            this.removeFieldError(this.confirmPasswordInput);
        } else {
            this.confirmPasswordInput.classList.remove('is-valid');
            this.confirmPasswordInput.classList.add('is-invalid');
            this.showFieldError(this.confirmPasswordInput, 'Passwords do not match');
        }
    }

    setupPasswordToggle() {
        const toggleButtons = document.querySelectorAll('.password-toggle');

        toggleButtons.forEach(button => {
            button.addEventListener('click', (e) => {
                e.preventDefault();
                const input = e.target.closest('.input-group').querySelector('.form-input');
                const icon = e.target.closest('button').querySelector('i');

                if (input.type === 'password') {
                    input.type = 'text';
                    icon.classList.remove('bi-eye');
                    icon.classList.add('bi-eye-slash');
                    button.setAttribute('aria-label', 'Hide password');
                } else {
                    input.type = 'password';
                    icon.classList.remove('bi-eye-slash');
                    icon.classList.add('bi-eye');
                    button.setAttribute('aria-label', 'Show password');
                }
            });
        });
    }

    handleRoleChange(selectedRole) {
        if (!this.companyField) return;

        if (selectedRole === 'Organizer') {
            this.companyField.classList.add('show');
            const companyInput = this.companyField.querySelector('input');
            if (companyInput) {
                companyInput.setAttribute('required', 'required');
            }
        } else {
            this.companyField.classList.remove('show');
            const companyInput = this.companyField.querySelector('input');
            if (companyInput) {
                companyInput.removeAttribute('required');
                companyInput.value = '';
                this.removeFieldError(companyInput);
                companyInput.classList.remove('is-invalid', 'is-valid');
            }
        }
    }

    setupRealTimeValidation() {
        const inputs = document.querySelectorAll('.form-input');

        inputs.forEach(input => {
            input.addEventListener('blur', () => {
                this.validateField(input);
            });

            input.addEventListener('input', () => {
                if (input.classList.contains('is-invalid')) {
                    this.validateField(input);
                }
            });
        });
    }

    validateField(field) {
        const value = field.value.trim();
        const fieldName = field.getAttribute('name') || field.getAttribute('id');
        let isValid = true;
        let errorMessage = '';

        // Required field validation
        if (field.hasAttribute('required') && !value) {
            isValid = false;
            errorMessage = `${this.getFieldLabel(field)} is required`;
        }

        // Email validation
        if (field.type === 'email' && value && !this.isValidEmail(value)) {
            isValid = false;
            errorMessage = 'Please enter a valid email address';
        }

        // Phone validation (basic)
        if (field.type === 'tel' && value && !this.isValidPhone(value)) {
            isValid = false;
            errorMessage = 'Please enter a valid phone number';
        }

        // Password validation (basic - works with existing backend)
        if (field.type === 'password' && fieldName === 'Password' && value) {
            if (value.length < 6) {
                isValid = false;
                errorMessage = 'Password must be at least 6 characters long';
            }
        }

        // Update field appearance
        if (isValid) {
            field.classList.remove('is-invalid');
            field.classList.add('is-valid');
            this.removeFieldError(field);
        } else {
            field.classList.remove('is-valid');
            field.classList.add('is-invalid');
            this.showFieldError(field, errorMessage);
        }

        return isValid;
    }

    isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    isValidPhone(phone) {
        // Basic phone validation - adjust as needed
        const phoneRegex = /^[\+]?[\d\s\-\(\)]{10,}$/;
        return phoneRegex.test(phone.replace(/\s/g, ''));
    }

    getFieldLabel(field) {
        const label = document.querySelector(`label[for="${field.id}"]`);
        if (label) {
            return label.textContent.replace('*', '').trim();
        }

        // Fallback to name attribute
        const name = field.getAttribute('name');
        if (name) {
            return name.charAt(0).toUpperCase() + name.slice(1);
        }

        return 'This field';
    }

    showFieldError(field, message) {
        this.removeFieldError(field);

        const errorElement = document.createElement('div');
        errorElement.className = 'field-validation-error';
        errorElement.textContent = message;
        errorElement.setAttribute('role', 'alert');

        // Insert after the input group
        const inputGroup = field.closest('.input-group');
        if (inputGroup) {
            inputGroup.parentNode.appendChild(errorElement);
        } else {
            field.parentNode.appendChild(errorElement);
        }
    }

    removeFieldError(field) {
        const inputGroup = field.closest('.input-group');
        const container = inputGroup ? inputGroup.parentNode : field.parentNode;
        const existingError = container.querySelector('.field-validation-error');
        if (existingError) {
            existingError.remove();
        }
    }

    handleFormSubmit(e) {
        const submitButton = this.form.querySelector('button[type="submit"]');
        const inputs = this.form.querySelectorAll('.form-input[required]');
        let isFormValid = true;

        // Validate all required fields
        inputs.forEach(input => {
            if (!this.validateField(input)) {
                isFormValid = false;
            }
        });

        // Check password match for registration forms
        if (this.confirmPasswordInput) {
            this.checkPasswordMatch();
            if (this.confirmPasswordInput.classList.contains('is-invalid')) {
                isFormValid = false;
            }
        }

        // Check terms checkbox for registration
        const termsCheckbox = document.getElementById('terms');
        if (termsCheckbox && !termsCheckbox.checked) {
            isFormValid = false;
            this.showFormError('Please accept the Terms of Service and Privacy Policy');
        }

        if (isFormValid) {
            // Show loading state
            this.setLoadingState(submitButton, true);

            // Let the form submit naturally to existing backend
            return true;
        } else {
            // Prevent submission and focus on first invalid field
            e.preventDefault();
            const firstInvalidField = this.form.querySelector('.is-invalid');
            if (firstInvalidField) {
                firstInvalidField.focus();
                firstInvalidField.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
            return false;
        }
    }

    showFormError(message) {
        // Create or update form-level error message
        let errorContainer = document.querySelector('.form-error-message');
        if (!errorContainer) {
            errorContainer = document.createElement('div');
            errorContainer.className = 'alert alert-danger form-error-message';
            errorContainer.setAttribute('role', 'alert');
            this.form.insertBefore(errorContainer, this.form.firstChild);
        }
        errorContainer.textContent = message;
    }

    setLoadingState(button, isLoading) {
        if (isLoading) {
            button.classList.add('btn-loading');
            button.disabled = true;
            button.setAttribute('data-original-text', button.innerHTML);
            button.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Processing...';
        } else {
            button.classList.remove('btn-loading');
            button.disabled = false;
            const originalText = button.getAttribute('data-original-text');
            if (originalText) {
                button.innerHTML = originalText;
            }
        }
    }

    setupAccessibility() {
        // Add ARIA labels and descriptions
        const passwordInput = document.getElementById('password');
        if (passwordInput && this.strengthIndicator) {
            passwordInput.setAttribute('aria-describedby', 'password-strength');
            this.strengthIndicator.setAttribute('id', 'password-strength');
            this.strengthIndicator.setAttribute('aria-live', 'polite');
        }

        // Ensure all interactive elements are keyboard accessible
        const interactiveElements = document.querySelectorAll('button, input, select, textarea, a[href]');
        interactiveElements.forEach(element => {
            if (!element.hasAttribute('tabindex') && !element.disabled) {
                element.setAttribute('tabindex', '0');
            }
        });

        // Add keyboard navigation for role selection
        this.roleInputs?.forEach((input, index) => {
            input.addEventListener('keydown', (e) => {
                if (e.key === 'ArrowRight' || e.key === 'ArrowDown') {
                    e.preventDefault();
                    const nextIndex = (index + 1) % this.roleInputs.length;
                    this.roleInputs[nextIndex].focus();
                } else if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') {
                    e.preventDefault();
                    const prevIndex = (index - 1 + this.roleInputs.length) % this.roleInputs.length;
                    this.roleInputs[prevIndex].focus();
                }
            });
        });
    }

    setupVisualEnhancements() {
        // Add entrance animations
        const authCard = document.querySelector('.auth-card');
        if (authCard) {
            authCard.style.opacity = '0';
            authCard.style.transform = 'translateY(20px)';

            setTimeout(() => {
                authCard.style.transition = 'all 0.6s cubic-bezier(0.4, 0, 0.2, 1)';
                authCard.style.opacity = '1';
                authCard.style.transform = 'translateY(0)';
            }, 100);
        }

        // Add focus effects to form inputs
        const inputs = document.querySelectorAll('.form-input');
        inputs.forEach(input => {
            input.addEventListener('focus', () => {
                input.closest('.input-group').classList.add('focused');
            });

            input.addEventListener('blur', () => {
                input.closest('.input-group').classList.remove('focused');
            });
        });

        // Add ripple effect to buttons
        this.addRippleEffect();
    }

    addRippleEffect() {
        const buttons = document.querySelectorAll('.btn-auth, .btn-social');

        buttons.forEach(button => {
            button.addEventListener('click', function (e) {
                const ripple = document.createElement('span');
                const rect = this.getBoundingClientRect();
                const size = Math.max(rect.width, rect.height);
                const x = e.clientX - rect.left - size / 2;
                const y = e.clientY - rect.top - size / 2;

                ripple.style.width = ripple.style.height = size + 'px';
                ripple.style.left = x + 'px';
                ripple.style.top = y + 'px';
                ripple.classList.add('ripple');

                this.appendChild(ripple);

                setTimeout(() => {
                    ripple.remove();
                }, 600);
            });
        });
    }
}

// Utility functions for enhanced UX
class UIEnhancements {
    static init() {
        this.setupKeyboardShortcuts();
        this.setupFormAnimations();
        this.setupSmoothScrolling();
    }

    static setupKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            // Escape to clear form errors
            if (e.key === 'Escape') {
                const errors = document.querySelectorAll('.field-validation-error, .form-error-message');
                errors.forEach(error => {
                    error.style.opacity = '0.5';
                    setTimeout(() => {
                        error.style.opacity = '1';
                    }, 300);
                });
            }

            // Enter to submit form (but let normal form submission handle it)
            if (e.key === 'Enter' && e.target.tagName !== 'TEXTAREA' && e.target.type !== 'submit') {
                const form = e.target.closest('form');
                if (form) {
                    const submitButton = form.querySelector('button[type="submit"]');
                    if (submitButton && !submitButton.disabled) {
                        submitButton.click();
                    }
                }
            }
        });
    }

    static setupFormAnimations() {
        // Animate form fields on focus
        const formGroups = document.querySelectorAll('.form-group');

        formGroups.forEach((group, index) => {
            group.style.opacity = '0';
            group.style.transform = 'translateY(20px)';

            setTimeout(() => {
                group.style.transition = 'all 0.4s cubic-bezier(0.4, 0, 0.2, 1)';
                group.style.opacity = '1';
                group.style.transform = 'translateY(0)';
            }, index * 100);
        });
    }

    static setupSmoothScrolling() {
        // Smooth scrolling for anchor links
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                e.preventDefault();
                const target = document.querySelector(this.getAttribute('href'));
                if (target) {
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });
    }
}

// Global utility functions
window.EventHubAuth = {
    // Function to show coming soon modal
    showComingSoon: function (featureName) {
        const modalElement = document.getElementById('comingSoonModal');
        if (modalElement) {
            document.getElementById('featureName').textContent = featureName;
            const modal = new bootstrap.Modal(modalElement);
            modal.show();
        }
    },

    // Function to show terms modal
    showTermsModal: function () {
        const modalElement = document.getElementById('termsModal');
        if (modalElement) {
            const modal = new bootstrap.Modal(modalElement);
            modal.show();
        }
    },

    // Function to show privacy modal
    showPrivacyModal: function () {
        const modalElement = document.getElementById('privacyModal');
        if (modalElement) {
            const modal = new bootstrap.Modal(modalElement);
            modal.show();
        }
    },

    // Function to validate form manually
    validateForm: function () {
        const authHandler = window.authHandler;
        if (authHandler) {
            const form = document.querySelector('.auth-form');
            const inputs = form.querySelectorAll('.form-input[required]');
            let isValid = true;

            inputs.forEach(input => {
                if (!authHandler.validateField(input)) {
                    isValid = false;
                }
            });

            return isValid;
        }
        return false;
    }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Create global instance
    window.authHandler = new AuthHandler();

    // Initialize enhancements
    UIEnhancements.init();

    // Add global CSS for animations and effects
    const style = document.createElement('style');
    style.textContent = `
        .focused .input-icon {
            color: var(--primary-color) !important;
            transform: translateY(-50%) scale(1.1);
        }
        
        .ripple {
            position: absolute;
            border-radius: 50%;
            background: rgba(255, 255, 255, 0.6);
            transform: scale(0);
            animation: ripple-animation 0.6s linear;
            pointer-events: none;
        }
        
        @keyframes ripple-animation {
            to {
                transform: scale(4);
                opacity: 0;
            }
        }
        
        .form-error-message {
            margin-bottom: 1rem;
            animation: slideIn 0.3s ease;
        }
        
        @keyframes slideIn {
            from {
                opacity: 0;
                transform: translateY(-10px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }
        
        /* Loading spinner */
        .spinner-border-sm {
            width: 1rem;
            height: 1rem;
        }
        
        /* Remove any yellow warning icons */
        .field-validation-error::before,
        .validation-summary-errors li::before {
            display: none !important;
        }
    `;
    document.head.appendChild(style);
});

// Export for potential use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        AuthHandler,
        UIEnhancements
    };
}
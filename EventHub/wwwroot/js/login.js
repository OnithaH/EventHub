// EventHub Login Page JavaScript
// File: wwwroot/js/login.js
// Compatible with ASP.NET Core 8.0

(function () {
    'use strict';

    // Global variables
    var selectedRole = 'Customer';
    var isInitialized = false;

    // Initialize login page when DOM is ready
    function initializeLogin() {
        if (isInitialized) return;
        isInitialized = true;

        // Get initial role from hidden field or default to Customer
        var roleField = document.querySelector('input[name="Role"]');
        if (roleField && roleField.value) {
            selectedRole = roleField.value;
        } else {
            selectedRole = 'Customer';
            switchRole('Customer');
        }

        // Auto-hide success alert after 10 seconds
        var successAlert = document.getElementById('registrationSuccess');
        if (successAlert) {
            setTimeout(function () {
                successAlert.style.display = 'none';
            }, 10000);
        }

        // Setup form validation
        setupFormValidation();

        // Setup event listeners
        setupEventListeners();
    }

    // Setup form validation
    function setupFormValidation() {
        var form = document.getElementById('loginForm');
        if (!form) return;

        var inputs = form.querySelectorAll('input[type="email"], input[type="password"]');

        inputs.forEach(function (input) {
            input.addEventListener('blur', validateField);
            input.addEventListener('input', clearFieldError);
        });

        // Enhanced form submission
        form.addEventListener('submit', function (event) {
            var submitBtn = form.querySelector('button[type="submit"]');

            if (!submitBtn.disabled) {
                var originalText = submitBtn.innerHTML;

                submitBtn.disabled = true;
                submitBtn.innerHTML = '<i class="bi bi-hourglass-split me-2"></i>Signing In...';
                submitBtn.classList.add('loading');

                // Reset button after delay if form validation fails
                setTimeout(function () {
                    if (submitBtn.disabled && submitBtn.classList.contains('loading')) {
                        submitBtn.disabled = false;
                        submitBtn.innerHTML = originalText;
                        submitBtn.classList.remove('loading');
                    }
                }, 5000);
            }
        });
    }

    // Setup event listeners
    function setupEventListeners() {
        // Role tab buttons
        var tabButtons = document.querySelectorAll('.tab-button');
        tabButtons.forEach(function (button) {
            button.addEventListener('click', function () {
                var role = this.getAttribute('data-role');
                if (role) {
                    switchRole(role);
                }
            });
        });

        // Password toggle button
        var passwordToggle = document.querySelector('.password-toggle');
        if (passwordToggle) {
            passwordToggle.addEventListener('click', togglePassword);
        }

        // Forgot password link
        var forgotPasswordLink = document.querySelector('.forgot-password-link');
        if (forgotPasswordLink) {
            forgotPasswordLink.addEventListener('click', function (e) {
                e.preventDefault();
                showForgotPassword();
            });
        }
    }

    // Validate individual form field
    function validateField(event) {
        var field = event.target;
        var value = field.value.trim();

        clearFieldError(event);

        if (field.required && !value) {
            showFieldError(field, 'This field is required');
            return false;
        }

        if (field.type === 'email' && value) {
            var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(value)) {
                showFieldError(field, 'Please enter a valid email address');
                return false;
            }
        }

        if (field.type === 'password' && value) {
            if (value.length < 6) {
                showFieldError(field, 'Password must be at least 6 characters');
                return false;
            }
        }

        return true;
    }

    // Show field error message
    function showFieldError(field, message) {
        field.classList.add('is-invalid');
        var feedback = field.parentNode.querySelector('.text-danger') ||
            field.parentNode.parentNode.querySelector('.text-danger');
        if (feedback) {
            feedback.textContent = message;
            feedback.style.display = 'block';
        }
    }

    // Clear field error message
    function clearFieldError(event) {
        var field = event.target;
        field.classList.remove('is-invalid');

        var feedback = field.parentNode.querySelector('.text-danger') ||
            field.parentNode.parentNode.querySelector('.text-danger');
        if (feedback && feedback.textContent !== '') {
            // Only clear client-side validation errors, keep server-side errors
            var serverErrors = ['Invalid email or password', 'Invalid role selected for this account'];
            var hasServerError = serverErrors.some(function (error) {
                return feedback.textContent.includes(error);
            });
            if (!hasServerError) {
                feedback.style.display = 'none';
            }
        }
    }

    // Switch user role
    function switchRole(role) {
        selectedRole = role;

        // Update tab buttons
        document.querySelectorAll('.tab-button').forEach(function (button) {
            button.classList.remove('active');
        });
        var activeButton = document.querySelector('[data-role="' + role + '"]');
        if (activeButton) {
            activeButton.classList.add('active');
        }

        // Update hidden role field
        var roleField = document.querySelector('input[name="Role"]');
        if (roleField) {
            roleField.value = role;
        }

        // Update form title based on role
        var formTitle = document.querySelector('.form-title');
        if (formTitle) {
            var roleTitles = {
                Customer: 'Sign In',
                Organizer: 'Organizer Sign In',
                Admin: 'Admin Access'
            };
            formTitle.textContent = roleTitles[role] || 'Sign In';
        }
    }

    // Toggle password visibility
    function togglePassword() {
        var field = document.querySelector('input[name="Password"]');
        var toggle = document.querySelector('.password-toggle i');

        if (field && toggle) {
            if (field.type === 'password') {
                field.type = 'text';
                toggle.classList.remove('bi-eye');
                toggle.classList.add('bi-eye-slash');
            } else {
                field.type = 'password';
                toggle.classList.remove('bi-eye-slash');
                toggle.classList.add('bi-eye');
            }
        }
    }

    // Show forgot password dialog
    function showForgotPassword() {
        if (typeof Swal === 'undefined') {
            alert('Please enter your email address to reset your password.');
            return;
        }

        Swal.fire({
            title: 'Reset Your Password',
            html: '<div class="mb-3">' +
                '<label for="resetEmail" class="form-label text-start w-100">Enter your email address:</label>' +
                '<input type="email" class="form-control" id="resetEmail" placeholder="your-email@example.com">' +
                '</div>' +
                '<div class="alert alert-info">' +
                '<small>We will send you a link to reset your password.</small>' +
                '</div>',
            showCancelButton: true,
            confirmButtonText: 'Send Reset Link',
            confirmButtonColor: '#6366f1',
            preConfirm: function () {
                var email = document.getElementById('resetEmail').value.trim();
                if (!email) {
                    Swal.showValidationMessage('Please enter your email address');
                    return false;
                }
                var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
                if (!emailRegex.test(email)) {
                    Swal.showValidationMessage('Please enter a valid email address');
                    return false;
                }
                return email;
            }
        }).then(function (result) {
            if (result.isConfirmed) {
                Swal.fire({
                    title: 'Reset Link Sent!',
                    html: '<div class="text-center">' +
                        '<div class="mb-3">' +
                        '<i class="bi bi-envelope-check text-success" style="font-size: 3rem;"></i>' +
                        '</div>' +
                        '<p>We have sent a password reset link to:</p>' +
                        '<strong>' + result.value + '</strong>' +
                        '<div class="alert alert-info mt-3">' +
                        '<small>Please check your email and follow the instructions to reset your password.</small>' +
                        '</div>' +
                        '</div>',
                    icon: 'success',
                    confirmButtonColor: '#6366f1'
                });
            }
        });
    }

    // Show login failed message
    function showLoginFailed() {
        var form = document.getElementById('loginForm');
        if (form) {
            form.classList.add('shake');
            setTimeout(function () {
                form.classList.remove('shake');
            }, 820);
        }

        if (typeof Swal !== 'undefined') {
            Swal.fire({
                title: 'Login Failed',
                text: 'Please check your credentials and try again.',
                icon: 'error',
                confirmButtonColor: '#6366f1',
                confirmButtonText: 'Try Again'
            });
        }
    }

    // Show success message
    function showSuccessMessage(message) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                title: 'Success!',
                text: message || 'Operation completed successfully!',
                icon: 'success',
                confirmButtonColor: '#6366f1'
            });
        }
    }

    // Export functions to global scope for Razor page integration
    window.EventHubLogin = {
        initialize: initializeLogin,
        switchRole: switchRole,
        togglePassword: togglePassword,
        showForgotPassword: showForgotPassword,
        showLoginFailed: showLoginFailed,
        showSuccessMessage: showSuccessMessage
    };

    // Auto-initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeLogin);
    } else {
        initializeLogin();
    }

})();
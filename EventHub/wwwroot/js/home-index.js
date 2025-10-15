// ==========================================
// EventHub Home Page - Index.cshtml Scripts
// ==========================================

document.addEventListener('DOMContentLoaded', function () {
    initializeHomePage();
});

/**
 * Initialize all home page functionality
 */
function initializeHomePage() {
    initializeSearchForm();
    initializeAnimations();
    initializeScrollEffects();
    initializeEventCards();
}

/**
 * Initialize search form functionality
 */
function initializeSearchForm() {
    const searchForm = document.querySelector('.search-form');

    if (searchForm) {
        // Form submission is handled by ASP.NET MVC
        // Add any client-side validation or enhancement here

        const categorySelect = document.getElementById('searchCategory');
        const locationSelect = document.getElementById('searchLocation');
        const dateInput = document.getElementById('searchDate');

        // Optional: Store last search parameters
        if (categorySelect) {
            categorySelect.addEventListener('change', function () {
                console.log('Category selected:', this.value);
            });
        }

        if (locationSelect) {
            locationSelect.addEventListener('change', function () {
                console.log('Location selected:', this.value);
            });
        }

        if (dateInput) {
            // Set minimum date to today
            const today = new Date().toISOString().split('T')[0];
            dateInput.setAttribute('min', today);
        }
    }
}

/**
 * Initialize scroll-based animations
 */
function initializeAnimations() {
    // Animate elements on scroll
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver(function (entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('fade-in-up');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    // Observe stat cards
    const statCards = document.querySelectorAll('.stat-card');
    statCards.forEach(card => observer.observe(card));

    // Observe event cards
    const eventCards = document.querySelectorAll('.event-card');
    eventCards.forEach(card => observer.observe(card));

    // Observe role cards
    const roleCards = document.querySelectorAll('.role-card');
    roleCards.forEach(card => observer.observe(card));
}

/**
 * Initialize scroll effects
 */
function initializeScrollEffects() {
    // Add scroll-to-top button
    createScrollToTopButton();

    // Handle scroll events
    let lastScroll = 0;
    window.addEventListener('scroll', function () {
        const currentScroll = window.pageYOffset;

        // Show/hide scroll to top button
        const scrollBtn = document.getElementById('scrollToTop');
        if (scrollBtn) {
            if (currentScroll > 300) {
                scrollBtn.style.display = 'flex';
            } else {
                scrollBtn.style.display = 'none';
            }
        }

        lastScroll = currentScroll;
    });
}

/**
 * Create scroll to top button
 */
function createScrollToTopButton() {
    // Check if button already exists
    if (document.getElementById('scrollToTop')) {
        return;
    }

    const scrollBtn = document.createElement('button');
    scrollBtn.id = 'scrollToTop';
    scrollBtn.className = 'btn-scroll-top';
    scrollBtn.innerHTML = '<i class="bi bi-arrow-up"></i>';
    scrollBtn.setAttribute('aria-label', 'Scroll to top');
    scrollBtn.style.display = 'none';

    scrollBtn.addEventListener('click', function () {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });

    document.body.appendChild(scrollBtn);

    // Add styles for scroll button
    addScrollButtonStyles();
}

/**
 * Add styles for scroll to top button
 */
function addScrollButtonStyles() {
    if (document.getElementById('scrollToTopStyles')) {
        return;
    }

    const style = document.createElement('style');
    style.id = 'scrollToTopStyles';
    style.textContent = `
        .btn-scroll-top {
            position: fixed;
            bottom: 2rem;
            right: 2rem;
            width: 50px;
            height: 50px;
            border-radius: 50%;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            border: none;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.25rem;
            cursor: pointer;
            z-index: 1000;
            box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);
            transition: all 0.3s ease;
        }
        
        .btn-scroll-top:hover {
            transform: translateY(-3px);
            box-shadow: 0 6px 20px rgba(102, 126, 234, 0.5);
        }
        
        @media (max-width: 768px) {
            .btn-scroll-top {
                bottom: 1rem;
                right: 1rem;
                width: 45px;
                height: 45px;
                font-size: 1.1rem;
            }
        }
    `;
    document.head.appendChild(style);
}

/**
 * Initialize event cards functionality
 */
function initializeEventCards() {
    const eventCards = document.querySelectorAll('.event-card');

    eventCards.forEach(card => {
        // Add hover effects
        card.addEventListener('mouseenter', function () {
            this.style.transition = 'all 0.3s ease';
        });

        // Track card clicks for analytics (optional)
        const viewDetailsBtn = card.querySelector('.btn-event');
        if (viewDetailsBtn) {
            viewDetailsBtn.addEventListener('click', function (e) {
                const eventTitle = card.querySelector('.event-title')?.textContent;
                console.log('Event card clicked:', eventTitle);
                // Add analytics tracking here if needed
            });
        }
    });
}

/**
 * Animate stats numbers (count up effect)
 */
function animateStatNumbers() {
    const statNumbers = document.querySelectorAll('.stat-number');

    statNumbers.forEach(stat => {
        const finalNumber = parseInt(stat.textContent.replace(/\D/g, ''));
        if (!isNaN(finalNumber)) {
            animateNumber(stat, 0, finalNumber, 2000);
        }
    });
}

/**
 * Animate a number from start to end
 */
function animateNumber(element, start, end, duration) {
    const range = end - start;
    const increment = range / (duration / 16); // 60 FPS
    let current = start;

    const timer = setInterval(() => {
        current += increment;
        if (current >= end) {
            current = end;
            clearInterval(timer);
        }
        element.textContent = Math.floor(current) + '+';
    }, 16);
}

/**
 * Handle role card selection
 */
function handleRoleCardClick(roleType) {
    console.log('Role selected:', roleType);
    // Role navigation is handled by anchor tags
}

/**
 * Initialize featured events carousel (if needed in future)
 */
function initializeEventCarousel() {
    // Placeholder for carousel functionality
    // Can be implemented if you want to add carousel in future
}

/**
 * Debounce function for performance optimization
 */
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

/**
 * Check if element is in viewport
 */
function isInViewport(element) {
    const rect = element.getBoundingClientRect();
    return (
        rect.top >= 0 &&
        rect.left >= 0 &&
        rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
        rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
}

// Optional: Add lazy loading for event images
function initializeLazyLoading() {
    const images = document.querySelectorAll('.event-image img');

    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    if (img.dataset.src) {
                        img.src = img.dataset.src;
                        img.removeAttribute('data-src');
                    }
                    imageObserver.unobserve(img);
                }
            });
        });

        images.forEach(img => imageObserver.observe(img));
    }
}

// Export functions for use in other scripts if needed
window.EventHubHome = {
    animateStatNumbers: animateStatNumbers,
    initializeSearchForm: initializeSearchForm
};
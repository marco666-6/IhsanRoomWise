// ============================================
// SIDEBAR FUNCTIONALITY
// ============================================

const sidebar = document.getElementById('sidebar');
const sidebarToggle = document.getElementById('sidebarToggle');

if (sidebarToggle) {
    sidebarToggle.addEventListener('click', function(e) {
        e.stopPropagation();
        sidebar.classList.toggle('collapsed');
        
        // Save state to localStorage
        localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
        
        // Mobile toggle
        if (window.innerWidth <= 768) {
            sidebar.classList.toggle('mobile-open');
        }
    });
}

// Restore sidebar state on page load
document.addEventListener('DOMContentLoaded', function() {
    const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
    if (isCollapsed && window.innerWidth > 768) {
        sidebar.classList.add('collapsed');
    }
    
    // Add tooltips to nav links for collapsed sidebar
    addNavTooltips();
});

// Add tooltip data attributes to nav links
function addNavTooltips() {
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
        const spanText = link.querySelector('span');
        if (spanText) {
            link.setAttribute('data-tooltip', spanText.textContent.trim());
        }
    });
}

// Close sidebar on mobile when clicking outside
document.addEventListener('click', function(e) {
    if (window.innerWidth <= 768) {
        if (sidebar && !sidebar.contains(e.target) && !sidebarToggle.contains(e.target)) {
            sidebar.classList.remove('mobile-open');
        }
    }
});

// Handle window resize
let resizeTimer;
window.addEventListener('resize', function() {
    clearTimeout(resizeTimer);
    resizeTimer = setTimeout(function() {
        if (window.innerWidth > 768) {
            sidebar.classList.remove('mobile-open');
        }
    }, 250);
});

// ============================================
// USER PROFILE DROPDOWN
// ============================================

const userProfileBtn = document.getElementById('userProfileBtn');
const userDropdown = document.querySelector('.user-profile-dropdown');

if (userProfileBtn) {
    userProfileBtn.addEventListener('click', function(e) {
        e.stopPropagation();
        userDropdown.classList.toggle('active');
    });
}

// Close dropdown when clicking outside
document.addEventListener('click', function(e) {
    if (userDropdown && !userDropdown.contains(e.target)) {
        userDropdown.classList.remove('active');
    }
});

// Close dropdown on escape key
document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape' && userDropdown) {
        userDropdown.classList.remove('active');
    }
});

// ============================================
// CURRENT TIME UPDATE
// ============================================

function updateTime() {
    const now = new Date();
    const options = {
        weekday: 'short',
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    };
    const timeString = now.toLocaleString('en-US', options);
    const timeElement = document.getElementById('currentTime');
    if (timeElement) {
        timeElement.textContent = timeString;
    }
}

// Update time immediately and then every minute
updateTime();
setInterval(updateTime, 60000);

// ============================================
// LOGOUT FUNCTIONALITY
// ============================================

const logoutLink = document.getElementById('logoutLink');
const logoutLinkDropdown = document.getElementById('logoutLinkDropdown');

function handleLogout(e) {
    e.preventDefault();

    const doLogout = function () {
        $.ajax({
            url: '/Auth/Logout',
            type: 'POST',
            success: function (res) {
                if (res.success) {
                    window.location.href = res.redirectUrl;
                } else {
                    alert(res.message || 'Logout failed');
                }
            },
            error: function () {
                alert('Logout error');
            }
        });
    };

    if (typeof Swal !== 'undefined') {
        Swal.fire({
            title: 'Logout?',
            text: 'Are you sure you want to logout?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#7aa22e',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Yes, logout',
            cancelButtonText: 'Cancel',
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {
                doLogout();
            }
        });
    } else {
        if (confirm('Are you sure you want to logout?')) {
            doLogout();
        }
    }
}

if (typeof $ !== 'undefined') {
    if (logoutLink) {
        $(logoutLink).on('click', handleLogout);
    }

    if (logoutLinkDropdown) {
        $(logoutLinkDropdown).on('click', handleLogout);
    }
}

// ============================================
// SMOOTH SCROLL BEHAVIOR
// ============================================

document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        const href = this.getAttribute('href');
        if (href !== '#' && href.length > 1) {
            e.preventDefault();
            const target = document.querySelector(href);
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        }
    });
});

// ============================================
// LOADING STATES FOR NAVIGATION
// ============================================

document.querySelectorAll('.nav-link').forEach(link => {
    link.addEventListener('click', function(e) {
        const href = this.getAttribute('href');
        if (href && href !== '#' && !href.startsWith('javascript:')) {
            const icon = this.querySelector('i');
            if (icon && !icon.classList.contains('fa-spinner')) {
                const originalClass = icon.className;
                icon.className = 'fas fa-spinner fa-spin';
                
                // Restore original icon after navigation starts
                setTimeout(() => {
                    icon.className = originalClass;
                }, 500);
            }
        }
    });
});

// ============================================
// ENHANCED BUTTON INTERACTIONS
// ============================================

// Add ripple effect to buttons and interactive elements
function createRipple(event) {
    const button = event.currentTarget;
    const ripple = document.createElement('span');
    const rect = button.getBoundingClientRect();
    const size = Math.max(rect.width, rect.height);
    const x = event.clientX - rect.left - size / 2;
    const y = event.clientY - rect.top - size / 2;
    
    ripple.style.width = ripple.style.height = size + 'px';
    ripple.style.left = x + 'px';
    ripple.style.top = y + 'px';
    ripple.classList.add('ripple');
    
    button.appendChild(ripple);
    
    setTimeout(() => {
        ripple.remove();
    }, 600);
}

// Add ripple effect CSS
const style = document.createElement('style');
style.textContent = `
    .ripple {
        position: absolute;
        border-radius: 50%;
        background: rgba(255, 255, 255, 0.5);
        transform: scale(0);
        animation: ripple-animation 0.6s ease-out;
        pointer-events: none;
    }
    
    @keyframes ripple-animation {
        to {
            transform: scale(4);
            opacity: 0;
        }
    }
    
    .sidebar-toggle,
    .user-profile-btn,
    .btn-primary,
    .btn-secondary {
        position: relative;
        overflow: hidden;
    }
`;
document.head.appendChild(style);

// Apply ripple effect
document.querySelectorAll('.sidebar-toggle, .user-profile-btn, .btn-primary, .btn-secondary').forEach(button => {
    button.addEventListener('click', createRipple);
});

// ============================================
// ACTIVE PAGE HIGHLIGHTING
// ============================================

// Highlight current page in navigation
function highlightCurrentPage() {
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.nav-link');
    
    navLinks.forEach(link => {
        const href = link.getAttribute('href');
        if (href && currentPath.includes(href)) {
            link.classList.add('active');
        }
    });
}

highlightCurrentPage();

// ============================================
// FORM ENHANCEMENTS
// ============================================

// Add floating label effect for form inputs
document.querySelectorAll('.form-control').forEach(input => {
    input.addEventListener('focus', function() {
        this.parentElement?.classList.add('focused');
    });
    
    input.addEventListener('blur', function() {
        if (!this.value) {
            this.parentElement?.classList.remove('focused');
        }
    });
    
    // Check initial state
    if (input.value) {
        input.parentElement?.classList.add('focused');
    }
});

// ============================================
// PERFORMANCE OPTIMIZATIONS
// ============================================

// Debounce function for performance
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

// Throttle function for performance
function throttle(func, limit) {
    let inThrottle;
    return function(...args) {
        if (!inThrottle) {
            func.apply(this, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
}

// ============================================
// ACCESSIBILITY ENHANCEMENTS
// ============================================

// Keyboard navigation for dropdown
if (userDropdown) {
    const dropdownItems = userDropdown.querySelectorAll('.dropdown-item');
    
    dropdownItems.forEach((item, index) => {
        item.addEventListener('keydown', function(e) {
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                const next = dropdownItems[index + 1];
                if (next) next.focus();
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                const prev = dropdownItems[index - 1];
                if (prev) prev.focus();
                else userProfileBtn.focus();
            }
        });
    });
}

// Tab trap for mobile sidebar
if (window.innerWidth <= 768) {
    sidebar?.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            sidebar.classList.remove('mobile-open');
            sidebarToggle?.focus();
        }
    });
}

// ============================================
// ANIMATION ON SCROLL
// ============================================

// Fade in elements on scroll
const observerOptions = {
    threshold: 0.1,
    rootMargin: '0px 0px -50px 0px'
};

const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            entry.target.style.animation = 'fadeInUp 0.6s ease forwards';
        }
    });
}, observerOptions);

// Observe cards and other elements
document.querySelectorAll('.card').forEach(card => {
    observer.observe(card);
});

// ============================================
// UTILITY FUNCTIONS
// ============================================

// Show toast notification
function showToast(message, type = 'info') {
    if (typeof Swal !== 'undefined') {
        const Toast = Swal.mixin({
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.addEventListener('mouseenter', Swal.stopTimer);
                toast.addEventListener('mouseleave', Swal.resumeTimer);
            }
        });
        
        Toast.fire({
            icon: type,
            title: message
        });
    }
}

// Make utility functions globally available
window.RoomWise = {
    showToast,
    debounce,
    throttle
};

$('select').not('.no-select2').select2({
    theme: 'bootstrap-5',
    placeholder: 'Select an option',
    allowClear: true
});

// ============================================
// CONSOLE BRANDING
// ============================================

console.log('%c🏢 RoomWise Management System', 'color: #7aa22e; font-size: 20px; font-weight: bold;');
console.log('%cVersion 2.0 - Enhanced UI', 'color: #5f7f23; font-size: 12px;');
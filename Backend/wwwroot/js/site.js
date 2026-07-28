// ===== Mobile Menu Toggle =====
function toggleMobileMenu() {
    const menu = document.getElementById('mobileMenu');
    menu.classList.toggle('active');
}

// Close mobile menu when clicking a link
document.addEventListener('DOMContentLoaded', function() {
    const mobileLinks = document.querySelectorAll('.mobile-menu .nav-link');
    mobileLinks.forEach(function(link) {
        link.addEventListener('click', function() {
            document.getElementById('mobileMenu').classList.remove('active');
        });
    });
});

// ===== Toast Notifications =====
function showToast(message, type = 'success') {
    const container = document.getElementById('toast-container');
    if (!container) return;
    const toast = document.createElement('div');
    toast.className = 'toast toast-' + type;
    toast.textContent = message;
    container.appendChild(toast);
    setTimeout(function() { toast.remove(); }, 4000);
}

// Auto-show toasts from alert elements on page load
document.addEventListener('DOMContentLoaded', function() {
    var alerts = document.querySelectorAll('.alert-info, .alert-error');
    alerts.forEach(function(alert) {
        var type = alert.classList.contains('alert-error') ? 'error' : 'success';
        showToast(alert.textContent.trim(), type);
    });
});

// ===== Modal Functions =====
function openModal(modalId) {
    var modal = document.getElementById(modalId);
    if (!modal) return;
    modal.classList.add('active');
    document.body.style.overflow = 'hidden';
    // Focus first input
    setTimeout(function() {
        var firstInput = modal.querySelector('input:not([type="hidden"]), textarea, select');
        if (firstInput) firstInput.focus();
    }, 100);
}

function closeModal(modalId) {
    var modal = document.getElementById(modalId);
    if (!modal) return;
    modal.classList.remove('active');
    document.body.style.overflow = '';
}

// Close modal on overlay click
document.addEventListener('click', function(e) {
    if (e.target.classList.contains('modal-overlay')) {
        e.target.classList.remove('active');
        document.body.style.overflow = '';
    }
});

// Close modal on Escape key
document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') {
        var activeModals = document.querySelectorAll('.modal-overlay.active');
        activeModals.forEach(function(modal) {
            modal.classList.remove('active');
        });
        document.body.style.overflow = '';
    }
});

// ===== Toggle Expand/Collapse =====
function toggleExpand(id) {
    var el = document.getElementById(id);
    if (!el) return;
    el.classList.toggle('hidden');
    // Update toggle button text
    var btn = event && event.currentTarget;
    if (btn) {
        var isHidden = el.classList.contains('hidden');
        var text = btn.textContent;
        // Toggle chevron direction is handled by CSS if needed
    }
}

// ===== Form Validation Helpers =====
document.addEventListener('DOMContentLoaded', function() {
    // Add loading state to forms on submit
    var forms = document.querySelectorAll('form');
    forms.forEach(function(form) {
        form.addEventListener('submit', function() {
            var submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn && !submitBtn.disabled) {
                submitBtn.disabled = true;
                var originalText = submitBtn.textContent;
                submitBtn.textContent = 'Processing...';
                // Re-enable after 5s as safety fallback
                setTimeout(function() {
                    submitBtn.disabled = false;
                    submitBtn.textContent = originalText;
                }, 5000);
            }
        });
    });
});

// ===== Smooth Scroll for Anchor Links =====
document.addEventListener('click', function(e) {
    var target = e.target.closest('a[href^="#"]');
    if (target) {
        var id = target.getAttribute('href').slice(1);
        var el = document.getElementById(id);
        if (el) {
            e.preventDefault();
            el.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }
});

// ===== Date Input Min Date (today) =====
document.addEventListener('DOMContentLoaded', function() {
    var dateInputs = document.querySelectorAll('input[type="date"]');
    var today = new Date().toISOString().split('T')[0];
    dateInputs.forEach(function(input) {
        if (!input.min) {
            input.min = today;
        }
    });
});

// ===================================================
// BiDE - Premium Interactions & Animations
// ===================================================

// ===== Navbar Scroll Effect =====
(function() {
    var navbar = document.querySelector('.navbar');
    if (!navbar) return;
    
    function onScroll() {
        if (window.scrollY > 50) {
            navbar.classList.add('scrolled');
        } else {
            navbar.classList.remove('scrolled');
        }
    }
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();
})();

// ===== Focus Management (shared by modals + drawer) =====
var FOCUSABLE_SELECTOR = 'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]):not([type="hidden"]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';

function getFocusable(container) {
    return Array.prototype.filter.call(
        container.querySelectorAll(FOCUSABLE_SELECTOR),
        function(el) { return el.offsetParent !== null || el === document.activeElement; }
    );
}

// Returns a keydown handler that keeps Tab focus inside the container.
function createFocusTrap(container) {
    return function(e) {
        if (e.key !== 'Tab') return;
        var focusable = getFocusable(container);
        if (!focusable.length) return;
        var first = focusable[0];
        var last = focusable[focusable.length - 1];
        if (e.shiftKey && document.activeElement === first) {
            e.preventDefault();
            last.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
            e.preventDefault();
            first.focus();
        }
    };
}

// ===== Hamburger Menu =====
var hamburgerTrap = null;
var hamburgerPreviousFocus = null;

function toggleHamburgerMenu() {
    var overlay = document.getElementById('hamburgerMenuOverlay');
    if (!overlay) return;
    hamburgerPreviousFocus = document.activeElement;
    overlay.classList.add('active');
    document.body.style.overflow = 'hidden';
    var drawer = overlay.querySelector('.hamburger-menu') || overlay;
    hamburgerTrap = createFocusTrap(drawer);
    overlay.addEventListener('keydown', hamburgerTrap);
    var closeBtn = overlay.querySelector('.hamburger-menu-close');
    if (closeBtn) setTimeout(function() { closeBtn.focus(); }, 100);
}

function closeHamburgerMenu() {
    var overlay = document.getElementById('hamburgerMenuOverlay');
    if (!overlay) return;
    overlay.classList.remove('active');
    document.body.style.overflow = '';
    if (hamburgerTrap) { overlay.removeEventListener('keydown', hamburgerTrap); hamburgerTrap = null; }
    if (hamburgerPreviousFocus && typeof hamburgerPreviousFocus.focus === 'function') {
        hamburgerPreviousFocus.focus();
    }
    hamburgerPreviousFocus = null;
}

function closeHamburgerIfOverlay(event) {
    if (event.target === document.getElementById('hamburgerMenuOverlay')) {
        closeHamburgerMenu();
    }
}

// Close menu on link click
document.addEventListener('DOMContentLoaded', function() {
    document.querySelectorAll('.hamburger-menu-link').forEach(function(link) {
        link.addEventListener('click', closeHamburgerMenu);
    });
});

// ===== Intersection Observer Animations =====
(function() {
    if (!('IntersectionObserver' in window)) return;
    
    var observer = new IntersectionObserver(function(entries) {
        entries.forEach(function(entry) {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1, rootMargin: '0px 0px -50px 0px' });

    document.addEventListener('DOMContentLoaded', function() {
        document.querySelectorAll('.animate-on-scroll').forEach(function(el) {
            observer.observe(el);
        });
    });
})();

// ===== FAQ Accordion =====
function toggleFaq(element) {
    var item = element.closest('.faq-item');
    if (!item) return;
    
    // Close others
    document.querySelectorAll('.faq-item.active').forEach(function(other) {
        if (other !== item) other.classList.remove('active');
    });
    
    item.classList.toggle('active');
}

// ===== Modal Functions =====
var modalTraps = {};        // modalId -> keydown handler
var modalPreviousFocus = {}; // modalId -> element to restore focus to

function openModal(modalId) {
    var modal = document.getElementById(modalId);
    if (!modal) return;
    modalPreviousFocus[modalId] = document.activeElement;
    modal.classList.add('active');
    document.body.style.overflow = 'hidden';
    var dialog = modal.querySelector('.modal') || modal;
    var trap = createFocusTrap(dialog);
    modalTraps[modalId] = trap;
    modal.addEventListener('keydown', trap);
    setTimeout(function() {
        var input = modal.querySelector('input:not([type="hidden"]), textarea, select');
        if (input) { input.focus(); }
        else {
            var first = getFocusable(dialog)[0];
            if (first) first.focus();
        }
    }, 100);
}

function closeModal(modalId) {
    var modal = document.getElementById(modalId);
    if (!modal) return;
    modal.classList.remove('active');
    document.body.style.overflow = '';
    if (modalTraps[modalId]) { modal.removeEventListener('keydown', modalTraps[modalId]); delete modalTraps[modalId]; }
    var prev = modalPreviousFocus[modalId];
    if (prev && typeof prev.focus === 'function') prev.focus();
    delete modalPreviousFocus[modalId];
}

// Close modal on overlay click
document.addEventListener('click', function(e) {
    if (e.target.classList.contains('modal-overlay')) {
        closeModal(e.target.id);
    }
});

// Close on Escape
document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') {
        var overlay = document.getElementById('hamburgerMenuOverlay');
        if (overlay && overlay.classList.contains('active')) {
            closeHamburgerMenu(); return;
        }
        document.querySelectorAll('.modal-overlay.active').forEach(function(m) {
            closeModal(m.id);
        });
    }
});

// ===== Toast Notifications =====
function showToast(message, type) {
    var container = document.getElementById('toast-container');
    if (!container) return;
    var toast = document.createElement('div');
    toast.className = 'toast toast-' + (type || 'success');
    toast.textContent = message;
    container.appendChild(toast);
    setTimeout(function() { toast.remove(); }, 4000);
}

document.addEventListener('DOMContentLoaded', function() {
    document.querySelectorAll('.alert-info, .alert-success, .alert-warning, .alert-error').forEach(function(alert) {
        var type;
        if (alert.classList.contains('alert-error')) { type = 'error'; }
        else if (alert.classList.contains('alert-warning')) { type = 'warning'; }
        else { type = 'success'; } // alert-info and alert-success both surface as success toasts
        showToast(alert.textContent.trim(), type);
        // CF-3: suppress the inline banner once mirrored into a toast (CSS hides .alert.toast-mirrored)
        alert.classList.add('toast-mirrored');
    });
});

// ===== Expand/Collapse =====
function toggleExpand(id) {
    var el = document.getElementById(id);
    if (el) el.classList.toggle('hidden');
}

// ===== Form Submit Loading =====
document.addEventListener('DOMContentLoaded', function() {
    function restoreSubmitButton(btn) {
        if (!btn || !btn.dataset.loading) return;
        btn.disabled = false;
        btn.innerHTML = btn.dataset.originalHtml || btn.innerHTML;
        delete btn.dataset.loading;
        delete btn.dataset.originalHtml;
    }

    document.querySelectorAll('form').forEach(function(form) {
        form.addEventListener('submit', function() {
            var btn = form.querySelector('button[type="submit"]');
            if (!btn || btn.disabled || btn.dataset.loading) return; // guard against double-submit
            btn.dataset.loading = '1';
            btn.dataset.originalHtml = btn.innerHTML;
            btn.innerHTML = '<span class="spinner"></span> Processing...';
            btn.disabled = true;
            // A full-page POST navigates away, which ends the loading state naturally.
        });
    });

    // Safety net: if navigation is cancelled or the page is restored from bfcache,
    // restore any button left in the loading state. No short timeout reset.
    function restoreAll() {
        document.querySelectorAll('button[data-loading]').forEach(restoreSubmitButton);
    }
    window.addEventListener('pageshow', restoreAll);
    window.addEventListener('pagehide', restoreAll);
});

// ===== Smooth Scroll =====
document.addEventListener('click', function(e) {
    var target = e.target.closest('a[href^="#"]');
    if (target) {
        var id = target.getAttribute('href').slice(1);
        var el = document.getElementById(id);
        if (el) { e.preventDefault(); el.scrollIntoView({ behavior: 'smooth', block: 'start' }); }
    }
});

// ===== Chat Assistant =====
function toggleChatWidget() {
    var panel = document.getElementById('chatPanel');
    var iconOpen = document.getElementById('chatIconOpen');
    var iconClose = document.getElementById('chatIconClose');
    if (!panel) return;
    var active = panel.classList.contains('active');
    panel.classList.toggle('active');
    if (iconOpen) iconOpen.style.display = active ? 'block' : 'none';
    if (iconClose) iconClose.style.display = active ? 'none' : 'block';
    if (!active) setTimeout(function() { var i = document.getElementById('chatInput'); if (i) i.focus(); }, 200);
}

function sendChatMessage() {
    var input = document.getElementById('chatInput');
    var messages = document.getElementById('chatMessages');
    if (!input || !messages) return;
    var text = input.value.trim();
    if (!text) return;
    var userMsg = document.createElement('div');
    userMsg.className = 'chat-msg chat-msg-user';
    userMsg.textContent = text;
    messages.appendChild(userMsg);
    input.value = '';
    messages.scrollTop = messages.scrollHeight;
    setTimeout(function() {
        var response = getChatResponse(text.toLowerCase());
        var botMsg = document.createElement('div');
        botMsg.className = 'chat-msg chat-msg-bot';
        botMsg.innerHTML = response;
        messages.appendChild(botMsg);
        messages.scrollTop = messages.scrollHeight;
    }, 600);
}

function getChatResponse(text) {
    if (text.includes('book') || text.includes('lesson')) return 'To book a lesson: Open the menu, go to <strong>Find Instructors</strong>, select an instructor, then click <strong>Book Now</strong> on their profile.';
    if (text.includes('pay') || text.includes('proof')) return 'After your booking is accepted, go to <strong>My Bookings</strong> and click <strong>Upload Proof of Payment</strong> to submit your receipt.';
    if (text.includes('instructor') && text.includes('find')) return 'Click <strong>Find Instructors</strong> from the menu to browse verified instructors by name or location.';
    if (text.includes('review')) return 'Go to <strong>Completed Lessons</strong> and click <strong>Leave Review</strong> to rate your instructor.';
    if (text.includes('progress')) return 'View your lesson progress from the menu under <strong>View Lesson Progress</strong>.';
    if (text.includes('register') || text.includes('sign up')) return 'Click <strong>Register</strong> to create an account as a Student or Instructor.';
    if (text.includes('cancel')) return 'Go to <strong>My Bookings</strong>, find the pending booking, and click <strong>Cancel</strong>.';
    if (text.includes('hello') || text.includes('hi')) return 'Hello! How can I help you today? Ask me about booking, payments, reviews, or anything else.';
    return 'I can help with: <strong>Booking lessons</strong>, <strong>Payments</strong>, <strong>Reviews</strong>, <strong>Progress tracking</strong>, and <strong>Account questions</strong>. Try asking a specific question!';
}


// ===== Navbar Typing Animation =====
(function() {
    var texts = ['BiDE', 'Drive. Book. Learn.', 'BiDE', 'Find Your Instructor', 'BiDE', 'Track Your Progress'];
    var el = document.getElementById('typingText');
    if (!el) return;
    var textIndex = 0;
    var charIndex = 0;
    var isDeleting = false;
    var typeSpeed = 100;
    var deleteSpeed = 60;
    var pauseAfterType = 2000;
    var pauseAfterDelete = 500;

    function type() {
        var current = texts[textIndex];
        if (!isDeleting) {
            el.textContent = current.substring(0, charIndex + 1);
            charIndex++;
            if (charIndex === current.length) {
                isDeleting = true;
                setTimeout(type, pauseAfterType);
            } else {
                setTimeout(type, typeSpeed);
            }
        } else {
            el.textContent = current.substring(0, charIndex - 1);
            charIndex--;
            if (charIndex === 0) {
                isDeleting = false;
                textIndex = (textIndex + 1) % texts.length;
                setTimeout(type, pauseAfterDelete);
            } else {
                setTimeout(type, deleteSpeed);
            }
        }
    }

    setTimeout(type, 1000);
})();

// ===== Top page-load progress bar =====
// Shows a slim bar at the top of the viewport whenever the user navigates
// (internal link click or form submit) so "loading anything" has feedback.
(function() {
    var bar = document.createElement('div');
    bar.id = 'page-progress';
    var started = false;
    var timer = null;

    function ready() {
        if (document.body) {
            document.body.appendChild(bar);
        } else {
            document.addEventListener('DOMContentLoaded', function() { document.body.appendChild(bar); });
        }
    }
    ready();

    function start() {
        if (started) return;
        started = true;
        bar.classList.add('active');
        bar.style.width = '0';
        // Force reflow so the width transition runs from 0.
        void bar.offsetWidth;
        var progress = 8;
        bar.style.width = progress + '%';
        timer = setInterval(function() {
            // Ease toward 90% but never finish until the page actually unloads.
            progress += (90 - progress) * 0.12;
            bar.style.width = progress.toFixed(1) + '%';
        }, 300);
    }

    function done() {
        if (!started) return;
        started = false;
        if (timer) { clearInterval(timer); timer = null; }
        bar.style.width = '100%';
        setTimeout(function() {
            bar.classList.remove('active');
            setTimeout(function() { bar.style.width = '0'; }, 300);
        }, 200);
    }

    // Internal link clicks (skip new-tab, downloads, hashes, external, modifier keys).
    document.addEventListener('click', function(e) {
        if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
        var a = e.target.closest ? e.target.closest('a') : null;
        if (!a) return;
        var href = a.getAttribute('href');
        if (!href || href.charAt(0) === '#') return;
        if (a.target && a.target !== '_self') return;
        if (a.hasAttribute('download')) return;
        if (a.getAttribute('rel') === 'external') return;
        // Same-origin only.
        if (a.host && a.host !== window.location.host) return;
        start();
    }, true);

    // Form submits navigate too (the submit-button spinner handles the button itself).
    document.addEventListener('submit', function(e) {
        if (e.defaultPrevented) return;
        var form = e.target;
        if (form && form.getAttribute('target') && form.getAttribute('target') !== '_self') return;
        if (form && form.id === 'antiforgery-form') return;
        start();
    }, true);

    // Finish on load and when returning via back/forward cache.
    window.addEventListener('load', done);
    window.addEventListener('pageshow', done);
    // If a navigation is cancelled (user hits Escape / stop), clear after a while.
    window.addEventListener('beforeunload', function() { /* keep bar visible until unload */ });
})();

// ===== Loading helpers for async content sections =====
// Usage: BiDE.showLoading(el, 'Loading bookings...')  /  BiDE.hideLoading(el)
window.BiDE = window.BiDE || {};
window.BiDE.showLoading = function(el, message) {
    if (!el) return;
    if (el.querySelector(':scope > .loading-overlay')) return;
    el.classList.add('is-loading');
    var ov = document.createElement('div');
    ov.className = 'loading-overlay';
    ov.innerHTML = '<span class="spinner-lg" role="status" aria-label="Loading"></span>' +
        (message ? '<p>' + String(message).replace(/[<>&]/g, '') + '</p>' : '');
    el.appendChild(ov);
};
window.BiDE.hideLoading = function(el) {
    if (!el) return;
    var ov = el.querySelector(':scope > .loading-overlay');
    if (ov) ov.remove();
    el.classList.remove('is-loading');
};

// ===== Password show/hide toggle =====
// Wraps every password input with an eye button that toggles visibility.
(function() {
    var EYE_ON = '<svg class="eye-on" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-7 11-7 11 7 11 7-4 7-11 7-11-7-11-7z"/><circle cx="12" cy="12" r="3"/></svg>';
    var EYE_OFF = '<svg class="eye-off" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/></svg>';

    function enhance(input) {
        if (!input || input.dataset.pwToggle) return;
        input.dataset.pwToggle = '1';
        var wrap = document.createElement('div');
        wrap.className = 'password-field';
        input.parentNode.insertBefore(wrap, input);
        wrap.appendChild(input);

        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'password-toggle';
        btn.setAttribute('aria-label', 'Show password');
        btn.setAttribute('aria-pressed', 'false');
        btn.innerHTML = EYE_ON + EYE_OFF;
        wrap.appendChild(btn);

        btn.addEventListener('click', function() {
            var show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            btn.classList.toggle('is-visible', show);
            btn.setAttribute('aria-pressed', show ? 'true' : 'false');
            btn.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
            input.focus();
        });
    }

    document.addEventListener('DOMContentLoaded', function() {
        document.querySelectorAll('input[type="password"]').forEach(enhance);
    });
})();

// ===== Logout confirmation =====
// Intercepts any link to /Account/Logout and asks the user to confirm first.
(function() {
    var pendingHref = null;

    function buildDialog() {
        if (document.getElementById('logoutConfirmModal')) return;
        var overlay = document.createElement('div');
        overlay.className = 'modal-overlay';
        overlay.id = 'logoutConfirmModal';
        overlay.innerHTML =
            '<div class="modal confirm-dialog" style="position:relative;" role="dialog" aria-modal="true" aria-labelledby="logoutConfirmTitle">' +
            '<button class="modal-close" type="button" aria-label="Close" data-logout-cancel>&times;</button>' +
            '<div class="confirm-icon"><svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg></div>' +
            '<h3 class="modal-title" id="logoutConfirmTitle">Log out of BiDE?</h3>' +
            '<p class="text-sm text-muted" style="margin-top:0.5rem;">You will need to sign in again to access your account.</p>' +
            '<div class="confirm-actions">' +
            '<button type="button" class="btn" data-logout-cancel>Stay signed in</button>' +
            '<a href="#" class="btn btn-destructive" id="logoutConfirmBtn">Log out</a>' +
            '</div></div>';
        document.body.appendChild(overlay);

        overlay.querySelectorAll('[data-logout-cancel]').forEach(function(el) {
            el.addEventListener('click', function() { closeModal('logoutConfirmModal'); });
        });
        overlay.querySelector('#logoutConfirmBtn').addEventListener('click', function(e) {
            // Let the top progress bar show, then navigate.
            if (pendingHref) { e.currentTarget.setAttribute('href', pendingHref); }
        });
    }

    document.addEventListener('click', function(e) {
        var a = e.target.closest ? e.target.closest('a') : null;
        if (!a) return;
        var href = a.getAttribute('href') || '';
        if (!/\/Account\/Logout\/?$/i.test(href)) return;
        e.preventDefault();
        pendingHref = href;
        buildDialog();
        var confirmBtn = document.getElementById('logoutConfirmBtn');
        if (confirmBtn) confirmBtn.setAttribute('href', href);
        openModal('logoutConfirmModal');
    }, true);
})();

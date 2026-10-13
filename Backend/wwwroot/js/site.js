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

// ===== Hamburger Menu =====
function toggleHamburgerMenu() {
    var overlay = document.getElementById('hamburgerMenuOverlay');
    if (!overlay) return;
    overlay.classList.add('active');
    document.body.style.overflow = 'hidden';
}

function closeHamburgerMenu() {
    var overlay = document.getElementById('hamburgerMenuOverlay');
    if (!overlay) return;
    overlay.classList.remove('active');
    document.body.style.overflow = '';
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
function openModal(modalId) {
    var modal = document.getElementById(modalId);
    if (!modal) return;
    modal.classList.add('active');
    document.body.style.overflow = 'hidden';
    setTimeout(function() {
        var input = modal.querySelector('input:not([type="hidden"]), textarea, select');
        if (input) input.focus();
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

// Close on Escape
document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') {
        var overlay = document.getElementById('hamburgerMenuOverlay');
        if (overlay && overlay.classList.contains('active')) {
            closeHamburgerMenu(); return;
        }
        document.querySelectorAll('.modal-overlay.active').forEach(function(m) {
            m.classList.remove('active');
        });
        document.body.style.overflow = '';
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
    document.querySelectorAll('.alert-info, .alert-error').forEach(function(alert) {
        var type = alert.classList.contains('alert-error') ? 'error' : 'success';
        showToast(alert.textContent.trim(), type);
    });
});

// ===== Expand/Collapse =====
function toggleExpand(id) {
    var el = document.getElementById(id);
    if (el) el.classList.toggle('hidden');
}

// ===== Form Submit Loading =====
document.addEventListener('DOMContentLoaded', function() {
    document.querySelectorAll('form').forEach(function(form) {
        form.addEventListener('submit', function() {
            var btn = form.querySelector('button[type="submit"]');
            if (btn && !btn.disabled) {
                btn.disabled = true;
                var original = btn.innerHTML;
                btn.innerHTML = '<span class="spinner"></span> Processing...';
                setTimeout(function() { btn.disabled = false; btn.innerHTML = original; }, 5000);
            }
        });
    });
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

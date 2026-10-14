// LiveMap.js - Student-facing real-time instructor map (Mapbox GL JS)
// Prototype-styled: green car pins, photo popups, rich sidebar cards, transmission filter.
// NOTE: Mapbox uses [longitude, latitude] order.

(function () {
    "use strict";

    mapboxgl.accessToken = window.MAPBOX_TOKEN;

    const map = new mapboxgl.Map({
        container: 'live-map',
        style: 'mapbox://styles/mapbox/streets-v12',
        center: [28.0473, -26.2041], // Johannesburg [lng, lat]
        zoom: 11
    });
    map.addControl(new mapboxgl.NavigationControl(), 'bottom-right');

    const statusEl = document.getElementById('map-status');
    const listEl = document.getElementById('instructor-list');
    const listEmpty = document.getElementById('list-empty');
    const listLoading = document.getElementById('list-loading');

    function hideSkeletons() {
        if (listLoading) listLoading.style.display = 'none';
    }
    const countEls = [
        document.getElementById('available-count'),
        document.getElementById('chip-count'),
        document.getElementById('side-count')
    ];
    const filterBar = document.getElementById('transmission-filters');

    const markers = {};        // instructorId -> { marker, data }
    const instructorData = {};
    let activeFilter = 'all';

    // ----- marker element (green teardrop car pin + pulse) -----
    function buildMarkerElement(id) {
        const wrap = document.createElement('button');
        wrap.className = 'map-pin';
        wrap.innerHTML =
            '<div class="pin-pulse"></div>' +
            '<div class="pin-body">' +
            '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M19 17h2v-5l-2-5H5L3 12v5h2"/><circle cx="7.5" cy="17.5" r="1.5"/><circle cx="16.5" cy="17.5" r="1.5"/></svg>' +
            '</div>';
        return wrap;
    }

    function esc(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    function initials(name) {
        var parts = String(name || '').trim().split(/\s+/).filter(Boolean);
        if (parts.length === 0) return '?';
        if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
        return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
    }

    function avatarHtml(d, cls) {
        var ini = esc(initials(d.name));
        var pic = (d.profilePicture || '').trim();
        // Only treat it as an image if it looks like a real URL/path.
        var looksLikePath = pic && (pic.charAt(0) === '/' || /^https?:\/\//i.test(pic));
        if (looksLikePath) {
            // data-fallback carries the initials; a delegated error handler swaps a
            // broken image for the initials box so a bad path never shows its alt text.
            return '<img src="' + esc(pic) + '" alt="' + esc(d.name) + '" class="avatar-img" data-fallback="' + ini + '" />';
        }
        return '<div class="avatar-fallback">' + ini + '</div>';
    }

    function money(d) {
        if (!d.price || d.price <= 0) return '<span class="price-tba">Price on request</span>';
        return 'R' + Math.round(d.price);
    }

    // When a profile image fails to load, replace it with the initials box.
    // (image 'error' does not bubble, so listen in the capture phase.)
    document.addEventListener('error', function (e) {
        var img = e.target;
        if (!img || img.tagName !== 'IMG' || !img.classList.contains('avatar-img')) return;
        var box = document.createElement('div');
        box.className = 'avatar-fallback';
        box.textContent = img.getAttribute('data-fallback') || '?';
        if (img.parentNode) img.parentNode.replaceChild(box, img);
    }, true);

    function ratingHtml(d) {
        if (!d.rating) return '';
        return '<span><svg width="9" height="9" viewBox="0 0 24 24"><path d="M12 2l3 6 6 .9-4.5 4.3 1 6.3L12 17l-5.5 2.5 1-6.3L3 8.9 9 8z"/></svg>' + d.rating.toFixed(2) + '</span>';
    }

    function buildPopupHtml(d) {
        return '<div class="mini-popup">' +
            '<div class="popup-top">' +
            avatarHtml(d) +
            '<div><strong>' + esc(d.name) + '</strong>' +
            '<span><svg width="9" height="9" viewBox="0 0 24 24"><path d="M12 2l3 6 6 .9-4.5 4.3 1 6.3L12 17l-5.5 2.5 1-6.3L3 8.9 9 8z"/></svg>' +
            (d.rating ? d.rating.toFixed(2) : 'New') + '</span></div>' +
            '</div>' +
            '<div class="popup-bottom">' +
            '<span>' + esc(d.vehicleType) + '</span>' +
            '<strong>' + money(d) + (d.price > 0 ? '<small>/hr</small>' : '') + '</strong>' +
            '</div>' +
            '<a href="/Instructors/Detail/' + d.instructorId + '" style="width:100%;border:0;border-radius:8px;padding:7px 10px;display:flex;justify-content:space-between;align-items:center;background:#2563eb;color:white;font-size:10px;font-weight:700;text-decoration:none;">View profile <span>&rarr;</span></a>' +
            '</div>';
    }

    // Center on student's location
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(function (pos) {
            map.flyTo({ center: [pos.coords.longitude, pos.coords.latitude], zoom: 13 });
            var youEl = document.createElement('div');
            youEl.className = 'map-you-dot';
            new mapboxgl.Marker(youEl).setLngLat([pos.coords.longitude, pos.coords.latitude]).addTo(map);
        });
    }

    // ----- SignalR -----
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/instructorHub")
        .withAutomaticReconnect()
        .build();

    connection.on("InstructorLocationUpdated", function (data) {
        instructorData[data.instructorId] = data;

        if (markers[data.instructorId]) {
            markers[data.instructorId].marker.setLngLat([data.longitude, data.latitude]);
            markers[data.instructorId].data = data;
        } else {
            const popup = new mapboxgl.Popup({ offset: 30, className: 'map-popup-wrap', closeButton: true, maxWidth: '280px' })
                .setHTML(buildPopupHtml(data));
            const marker = new mapboxgl.Marker(buildMarkerElement(data.instructorId))
                .setLngLat([data.longitude, data.latitude])
                .setPopup(popup)
                .addTo(map);
            markers[data.instructorId] = { marker: marker, data: data };
        }

        applyFilter();
        renderSidebar();
        updateCount();
    });

    connection.on("InstructorRemoved", function (instructorId) {
        if (markers[instructorId]) {
            markers[instructorId].marker.remove();
            delete markers[instructorId];
        }
        delete instructorData[instructorId];
        applyFilter();
        renderSidebar();
        updateCount();
    });

    connection.start()
        .then(function () {
            // Connected: stop showing skeletons. If no one is live yet, show the waiting text.
            hideSkeletons();
            if (Object.keys(instructorData).length === 0) {
                listEmpty.style.display = 'block';
            }
            showStatus("Connected. Waiting for available instructors...");
        })
        .catch(function (err) {
            hideSkeletons();
            listEmpty.style.display = 'block';
            showStatus("Unable to connect to live tracking. Please refresh.");
            console.error(err);
        });

    // ----- transmission filter -----
    filterBar.addEventListener('click', function (e) {
        var btn = e.target.closest('button');
        if (!btn) return;
        activeFilter = btn.dataset.filter;
        filterBar.querySelectorAll('button').forEach(function (b) { b.classList.remove('active'); });
        btn.classList.add('active');
        applyFilter();
        renderSidebar();
        updateCount();
    });

    function matchesFilter(d) {
        if (activeFilter === 'all') return true;
        return (d.vehicleType || '').toLowerCase() === activeFilter.toLowerCase();
    }

    function applyFilter() {
        Object.keys(markers).forEach(function (id) {
            var el = markers[id].marker.getElement();
            el.style.display = matchesFilter(markers[id].data) ? '' : 'none';
        });
    }

    function updateCount() {
        var n = Object.values(instructorData).filter(matchesFilter).length;
        countEls.forEach(function (el) { if (el) el.textContent = n; });
        if (n > 0) statusEl.style.display = 'none';
    }

    function renderSidebar() {
        var items = Object.values(instructorData).filter(matchesFilter);

        // clear
        listEl.querySelectorAll('.instructor-card').forEach(function (el) { el.remove(); });

        if (items.length === 0) {
            hideSkeletons();
            listEmpty.style.display = 'block';
            return;
        }
        hideSkeletons();
        listEmpty.style.display = 'none';

        items.forEach(function (d) {
            var card = document.createElement('div');
            card.className = 'instructor-card';
            card.innerHTML =
                '<div class="avatar-wrap">' + avatarHtml(d) + '<i></i></div>' +
                '<div class="instructor-info">' +
                '<div class="name-row"><strong>' + esc(d.name) + '</strong>' +
                '<span><svg width="8" height="8" viewBox="0 0 24 24"><path d="M12 2l3 6 6 .9-4.5 4.3 1 6.3L12 17l-5.5 2.5 1-6.3L3 8.9 9 8z"/></svg>' +
                (d.rating ? d.rating.toFixed(2) : 'New') + '</span></div>' +
                '<p>' + (d.lessonsCount || 0) + ' lesson' + ((d.lessonsCount || 0) === 1 ? '' : 's') + '</p>' +
                '<div class="card-meta">' +
                '<span>' + esc(d.vehicleType || 'Any') + '</span>' +
                '<strong>' + money(d) + (d.price > 0 ? '<small>/hr</small>' : '') + '</strong>' +
                '</div></div>' +
                '<button class="card-arrow" aria-label="View"><svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 18l6-6-6-6"/></svg></button>';

            card.addEventListener('click', function () {
                map.flyTo({ center: [d.longitude, d.latitude], zoom: 15 });
                if (markers[d.instructorId]) markers[d.instructorId].marker.togglePopup();
            });

            listEl.appendChild(card);
        });
    }

    function showStatus(msg) {
        statusEl.style.display = 'block';
        statusEl.textContent = msg;
    }
})();

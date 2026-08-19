// LiveMap.js - Student-facing real-time instructor map with sidebar

(function () {
    "use strict";

    const map = L.map('live-map').setView([-26.2041, 28.0473], 12);
    const statusEl = document.getElementById('map-status');
    const listEl = document.getElementById('instructor-list');
    const listEmpty = document.getElementById('list-empty');

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors',
        maxZoom: 18
    }).addTo(map);

    const markers = {};
    const instructorData = {};

    const carIcon = L.divIcon({
        html: '<div style="background:#22c55e;color:#fff;width:36px;height:36px;border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:18px;box-shadow:0 2px 8px rgba(0,0,0,0.3);border:2px solid #fff;">&#x1F697;</div>',
        className: '',
        iconSize: [36, 36],
        iconAnchor: [18, 18],
        popupAnchor: [0, -20]
    });

    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(function (pos) {
            map.setView([pos.coords.latitude, pos.coords.longitude], 13);
        });
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/instructorHub")
        .withAutomaticReconnect()
        .build();

    connection.on("InstructorLocationUpdated", function (data) {
        var key = data.instructorId;
        instructorData[key] = data;

        if (markers[key]) {
            markers[key].setLatLng([data.latitude, data.longitude]);
        } else {
            var marker = L.marker([data.latitude, data.longitude], { icon: carIcon }).addTo(map);

            marker.bindPopup(
                '<div style="min-width:180px;padding:4px;">' +
                '<p style="font-weight:700;margin:0 0 4px 0;font-size:14px;">' + data.name + '</p>' +
                '<p style="margin:0 0 8px 0;font-size:12px;color:#666;">Transmission: <strong>' + data.vehicleType + '</strong></p>' +
                '<form method="post" action="/Instructors/BookRealTime">' +
                '<input type="hidden" name="instructorId" value="' + data.instructorId + '" />' +
                '<input type="hidden" name="__RequestVerificationToken" value="' + (document.querySelector('input[name=__RequestVerificationToken]') ? document.querySelector('input[name=__RequestVerificationToken]').value : '') + '" />' +
                '<button type="submit" style="background:#2563eb;color:#fff;border:none;padding:8px 16px;border-radius:6px;cursor:pointer;width:100%;font-size:13px;font-weight:500;">Book Now</button>' +
                '</form></div>'
            );

            markers[key] = marker;
        }

        renderSidebar();
        showStatus(Object.keys(markers).length + " instructor(s) available nearby");
    });

    connection.on("InstructorRemoved", function (instructorId) {
        if (markers[instructorId]) {
            map.removeLayer(markers[instructorId]);
            delete markers[instructorId];
        }
        delete instructorData[instructorId];
        renderSidebar();
        showStatus(Object.keys(markers).length + " instructor(s) available nearby");
    });

    connection.start()
        .then(function () {
            showStatus("Connected. Waiting for available instructors...");
        })
        .catch(function (err) {
            showStatus("Unable to connect to live tracking. Please refresh the page.");
            console.error("SignalR connection error:", err);
        });

    function renderSidebar() {
        var keys = Object.keys(instructorData);

        if (keys.length === 0) {
            listEmpty.style.display = 'block';
            listEl.querySelectorAll('.sidebar-card').forEach(function(el) { el.remove(); });
            return;
        }

        listEmpty.style.display = 'none';

        // Remove cards that no longer exist
        listEl.querySelectorAll('.sidebar-card').forEach(function(el) {
            if (!instructorData[el.dataset.id]) el.remove();
        });

        keys.forEach(function(id) {
            var d = instructorData[id];
            var existing = listEl.querySelector('[data-id="' + id + '"]');

            if (!existing) {
                var card = document.createElement('div');
                card.className = 'sidebar-card';
                card.dataset.id = id;
                card.style.cssText = 'padding:0.75rem;margin-bottom:0.5rem;border:1px solid var(--border);border-radius:8px;cursor:pointer;transition:background 0.2s;';
                card.innerHTML =
                    '<p style="font-weight:600;font-size:0.875rem;margin:0;">' + d.name + '</p>' +
                    '<p style="font-size:0.75rem;opacity:0.6;margin:0.25rem 0 0 0;">' + d.vehicleType + '</p>';

                card.addEventListener('click', function() {
                    map.setView([d.latitude, d.longitude], 15);
                    markers[id].openPopup();
                });

                card.addEventListener('mouseenter', function() {
                    card.style.background = 'rgba(255,255,255,0.05)';
                });
                card.addEventListener('mouseleave', function() {
                    card.style.background = 'transparent';
                });

                listEl.appendChild(card);
            }
        });
    }

    function showStatus(msg) {
        statusEl.style.display = 'block';
        statusEl.textContent = msg;
    }
})();

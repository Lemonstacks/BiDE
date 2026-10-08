// LiveMap.js - Student-facing real-time instructor map with sidebar

(function () {
    "use strict";

    const map = L.map('live-map', { zoomControl: false }).setView([-26.2041, 28.0473], 12);

    // Dark themed tiles to match the app
    L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap &copy; CARTO',
        maxZoom: 20
    }).addTo(map);

    // Reposition zoom control to the top-right
    L.control.zoom({ position: 'topright' }).addTo(map);

    const statusEl = document.getElementById('map-status');
    const listEl = document.getElementById('instructor-list');
    const listEmpty = document.getElementById('list-empty');
    const countBadge = document.getElementById('available-count');

    const markers = {};
    const instructorData = {};

    function makeCarIcon() {
        return L.divIcon({
            html:
                '<div class="map-marker">' +
                '<div class="map-marker-pulse"></div>' +
                '<div class="map-marker-pin">' +
                '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#fff" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M19 17h2v-5l-2-5H5L3 12v5h2"/><circle cx="7.5" cy="17.5" r="1.5"/><circle cx="16.5" cy="17.5" r="1.5"/></svg>' +
                '</div></div>',
            className: 'map-marker-wrap',
            iconSize: [44, 44],
            iconAnchor: [22, 22],
            popupAnchor: [0, -24]
        });
    }

    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(function (pos) {
            map.setView([pos.coords.latitude, pos.coords.longitude], 13);

            // Mark the student's own location
            L.circleMarker([pos.coords.latitude, pos.coords.longitude], {
                radius: 8, color: '#2563eb', fillColor: '#2563eb', fillOpacity: 0.9, weight: 3
            }).addTo(map).bindPopup('<div class="map-popup"><strong>You are here</strong></div>');
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
            var marker = L.marker([data.latitude, data.longitude], { icon: makeCarIcon() }).addTo(map);

            marker.bindPopup(
                '<div class="map-popup">' +
                '<p class="map-popup-name">' + data.name + '</p>' +
                '<p class="map-popup-meta"><span class="map-popup-dot"></span>Available now</p>' +
                '<p class="map-popup-type">' + data.vehicleType + ' transmission</p>' +
                '<a href="/Instructors/Detail/' + data.instructorId + '" class="map-popup-btn">View Profile &rarr;</a>' +
                '</div>',
                { className: 'map-popup-wrap' }
            );

            markers[key] = marker;
        }

        renderSidebar();
        updateCount();
    });

    connection.on("InstructorRemoved", function (instructorId) {
        if (markers[instructorId]) {
            map.removeLayer(markers[instructorId]);
            delete markers[instructorId];
        }
        delete instructorData[instructorId];
        renderSidebar();
        updateCount();
    });

    connection.start()
        .then(function () {
            showStatus("Connected. Waiting for available instructors to appear...");
        })
        .catch(function (err) {
            showStatus("Unable to connect to live tracking. Please refresh the page.");
            console.error("SignalR connection error:", err);
        });

    function updateCount() {
        var n = Object.keys(instructorData).length;
        if (countBadge) countBadge.textContent = n;
        if (n > 0) { statusEl.style.display = 'none'; }
    }

    function renderSidebar() {
        var keys = Object.keys(instructorData);

        if (keys.length === 0) {
            listEmpty.style.display = 'block';
            listEl.querySelectorAll('.sidebar-card').forEach(function (el) { el.remove(); });
            return;
        }

        listEmpty.style.display = 'none';

        listEl.querySelectorAll('.sidebar-card').forEach(function (el) {
            if (!instructorData[el.dataset.id]) el.remove();
        });

        keys.forEach(function (id) {
            var d = instructorData[id];
            var existing = listEl.querySelector('[data-id="' + id + '"]');

            if (!existing) {
                var card = document.createElement('div');
                card.className = 'sidebar-card';
                card.dataset.id = id;
                card.innerHTML =
                    '<div class="sidebar-card-avatar">' + (d.name ? d.name.charAt(0).toUpperCase() : '?') + '</div>' +
                    '<div class="sidebar-card-info">' +
                    '<p class="sidebar-card-name">' + d.name + '</p>' +
                    '<p class="sidebar-card-type"><span class="sidebar-card-dot"></span>' + d.vehicleType + '</p>' +
                    '</div>' +
                    '<svg class="sidebar-card-arrow" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 18l6-6-6-6"/></svg>';

                card.addEventListener('click', function () {
                    map.setView([d.latitude, d.longitude], 15, { animate: true });
                    markers[id].openPopup();
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

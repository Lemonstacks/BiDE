// LiveMap.js - Student-facing real-time instructor map (Mapbox GL JS)
// NOTE: Mapbox uses [longitude, latitude] order (opposite of Leaflet).

(function () {
    "use strict";

    mapboxgl.accessToken = window.MAPBOX_TOKEN;

    const map = new mapboxgl.Map({
        container: 'live-map',
        style: 'mapbox://styles/mapbox/streets-v12',
        center: [28.0473, -26.2041], // Johannesburg [lng, lat]
        zoom: 11
    });

    map.addControl(new mapboxgl.NavigationControl(), 'top-right');

    const statusEl = document.getElementById('map-status');
    const listEl = document.getElementById('instructor-list');
    const listEmpty = document.getElementById('list-empty');
    const countBadge = document.getElementById('available-count');

    const markers = {};        // instructorId -> mapboxgl.Marker
    const instructorData = {}; // instructorId -> data

    function buildMarkerElement() {
        const el = document.createElement('div');
        el.className = 'map-marker';
        el.innerHTML =
            '<div class="map-marker-pulse"></div>' +
            '<div class="map-marker-pin">' +
            '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#fff" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M19 17h2v-5l-2-5H5L3 12v5h2"/><circle cx="7.5" cy="17.5" r="1.5"/><circle cx="16.5" cy="17.5" r="1.5"/></svg>' +
            '</div>';
        return el;
    }

    // Center on student's own location and drop a "you are here" marker
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(function (pos) {
            const lng = pos.coords.longitude;
            const lat = pos.coords.latitude;
            map.flyTo({ center: [lng, lat], zoom: 13 });

            const youEl = document.createElement('div');
            youEl.className = 'map-you-dot';
            new mapboxgl.Marker(youEl)
                .setLngLat([lng, lat])
                .setPopup(new mapboxgl.Popup({ offset: 16 }).setHTML('<div class="map-popup"><strong>You are here</strong></div>'))
                .addTo(map);
        });
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/instructorHub")
        .withAutomaticReconnect()
        .build();

    connection.on("InstructorLocationUpdated", function (data) {
        const key = data.instructorId;
        instructorData[key] = data;

        if (markers[key]) {
            markers[key].setLngLat([data.longitude, data.latitude]);
        } else {
            const popup = new mapboxgl.Popup({ offset: 28, className: 'map-popup-wrap' }).setHTML(
                '<div class="map-popup">' +
                '<p class="map-popup-name">' + data.name + '</p>' +
                '<p class="map-popup-meta"><span class="map-popup-dot"></span>Available now</p>' +
                '<p class="map-popup-type">' + data.vehicleType + ' transmission</p>' +
                '<a href="/Instructors/Detail/' + data.instructorId + '" class="map-popup-btn">View Profile &rarr;</a>' +
                '</div>'
            );

            const marker = new mapboxgl.Marker(buildMarkerElement())
                .setLngLat([data.longitude, data.latitude])
                .setPopup(popup)
                .addTo(map);

            markers[key] = marker;
        }

        renderSidebar();
        updateCount();
    });

    connection.on("InstructorRemoved", function (instructorId) {
        if (markers[instructorId]) {
            markers[instructorId].remove();
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
        const n = Object.keys(instructorData).length;
        if (countBadge) countBadge.textContent = n;
        if (n > 0) statusEl.style.display = 'none';
    }

    function renderSidebar() {
        const keys = Object.keys(instructorData);

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
            const d = instructorData[id];
            const existing = listEl.querySelector('[data-id="' + id + '"]');

            if (!existing) {
                const card = document.createElement('div');
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
                    map.flyTo({ center: [d.longitude, d.latitude], zoom: 15 });
                    markers[id].togglePopup();
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

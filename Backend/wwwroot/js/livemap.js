// LiveMap.js - Student-facing real-time instructor map

(function () {
    "use strict";

    const map = L.map('live-map').setView([-26.2041, 28.0473], 12); // Default: Johannesburg
    const statusEl = document.getElementById('map-status');

    // OpenStreetMap tiles
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors',
        maxZoom: 18
    }).addTo(map);

    // Track instructor markers
    const markers = {};

    // Car icon
    const carIcon = L.divIcon({
        html: '<div style="background:#22c55e;color:#fff;width:36px;height:36px;border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:18px;box-shadow:0 2px 8px rgba(0,0,0,0.3);border:2px solid #fff;">🚗</div>',
        className: '',
        iconSize: [36, 36],
        iconAnchor: [18, 18],
        popupAnchor: [0, -20]
    });

    // Center map on student's location
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(function (pos) {
            map.setView([pos.coords.latitude, pos.coords.longitude], 13);
        });
    }

    // SignalR connection
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/instructorHub")
        .withAutomaticReconnect()
        .build();

    connection.on("InstructorLocationUpdated", function (data) {
        const key = data.instructorId;

        if (markers[key]) {
            // Move existing marker
            markers[key].setLatLng([data.latitude, data.longitude]);
        } else {
            // Create new marker
            const marker = L.marker([data.latitude, data.longitude], { icon: carIcon }).addTo(map);

            marker.bindPopup(`
                <div style="min-width:180px;padding:4px;">
                    <p style="font-weight:700;margin:0 0 4px 0;font-size:14px;">${data.name}</p>
                    <p style="margin:0 0 8px 0;font-size:12px;color:#666;">Transmission: <strong>${data.vehicleType}</strong></p>
                    <form method="post" action="/Instructors/BookRealTime">
                        <input type="hidden" name="instructorId" value="${data.instructorId}" />
                        <input type="hidden" name="__RequestVerificationToken" value="${document.querySelector('input[name=__RequestVerificationToken]')?.value || ''}" />
                        <button type="submit" style="background:#2563eb;color:#fff;border:none;padding:8px 16px;border-radius:6px;cursor:pointer;width:100%;font-size:13px;font-weight:500;">Book Now</button>
                    </form>
                </div>
            `);

            markers[key] = marker;
        }

        showStatus(`${Object.keys(markers).length} instructor(s) available nearby`);
    });

    connection.on("InstructorRemoved", function (instructorId) {
        if (markers[instructorId]) {
            map.removeLayer(markers[instructorId]);
            delete markers[instructorId];
        }
        showStatus(`${Object.keys(markers).length} instructor(s) available nearby`);
    });

    connection.start()
        .then(function () {
            showStatus("Connected. Waiting for available instructors...");
        })
        .catch(function (err) {
            showStatus("Unable to connect to live tracking. Please refresh the page.");
            console.error("SignalR connection error:", err);
        });

    function showStatus(msg) {
        statusEl.style.display = 'block';
        statusEl.textContent = msg;
    }
})();

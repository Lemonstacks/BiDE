// GoLive.js - Instructor GPS broadcasting

(function () {
    "use strict";

    let connection = null;
    let watchId = null;
    let previewMap = null;
    let previewMarker = null;

    const btnLive = document.getElementById('btn-go-live');
    const btnOffline = document.getElementById('btn-go-offline');
    const statusOffline = document.getElementById('status-offline');
    const statusOnline = document.getElementById('status-online');
    const coordsDisplay = document.getElementById('coords-display');
    const mapPreview = document.getElementById('live-map-preview');

    window.goLive = async function () {
        if (!navigator.geolocation) {
            alert('Your browser does not support GPS location.');
            return;
        }

        // Build SignalR connection
        connection = new signalR.HubConnectionBuilder()
            .withUrl("/instructorHub")
            .withAutomaticReconnect()
            .build();

        try {
            await connection.start();
        } catch (err) {
            alert('Could not connect to tracking server. Please try again.');
            console.error(err);
            return;
        }

        // Start watching position
        watchId = navigator.geolocation.watchPosition(
            function (pos) {
                const lat = pos.coords.latitude;
                const lng = pos.coords.longitude;

                // Send to hub
                connection.invoke("UpdateLocation", {
                    instructorId: instructorId,
                    name: instructorName,
                    latitude: lat,
                    longitude: lng,
                    vehicleType: vehicleType,
                    isAvailable: true,
                    profilePicture: instructorPhoto,
                    rating: instructorRating,
                    lessonsCount: instructorLessons,
                    price: instructorPrice
                });

                coordsDisplay.textContent = `Lat: ${lat.toFixed(5)}, Lng: ${lng.toFixed(5)}`;

                // Update preview map
                updatePreviewMap(lat, lng);
            },
            function (err) {
                alert('Unable to get your location. Please enable GPS and try again.');
                console.error(err);
            },
            {
                enableHighAccuracy: true,
                maximumAge: 5000,
                timeout: 10000
            }
        );

        // Update UI
        btnLive.style.display = 'none';
        btnOffline.style.display = 'block';
        statusOffline.style.display = 'none';
        statusOnline.style.display = 'block';
        mapPreview.style.display = 'block';
    };

    window.goOffline = async function () {
        // Stop GPS watch
        if (watchId !== null) {
            navigator.geolocation.clearWatch(watchId);
            watchId = null;
        }

        // Notify hub
        if (connection) {
            try {
                await connection.invoke("GoOffline", instructorId);
                await connection.stop();
            } catch (err) {
                console.error(err);
            }
            connection = null;
        }

        // Update UI
        btnLive.style.display = 'block';
        btnOffline.style.display = 'none';
        statusOffline.style.display = 'block';
        statusOnline.style.display = 'none';
        mapPreview.style.display = 'none';
    };

    function updatePreviewMap(lat, lng) {
        if (!previewMap) {
            mapboxgl.accessToken = window.MAPBOX_TOKEN;
            previewMap = new mapboxgl.Map({
                container: 'live-map-preview',
                style: 'mapbox://styles/mapbox/streets-v12',
                center: [lng, lat],
                zoom: 15
            });

            var el = document.createElement('div');
            el.className = 'map-you-dot';
            previewMarker = new mapboxgl.Marker(el)
                .setLngLat([lng, lat])
                .setPopup(new mapboxgl.Popup({ offset: 16 }).setHTML('<div class="map-popup"><strong>You are here</strong></div>'))
                .addTo(previewMap);
        } else {
            previewMarker.setLngLat([lng, lat]);
            previewMap.panTo([lng, lat]);
        }
    }

    // Cleanup on page unload
    window.addEventListener('beforeunload', function () {
        if (connection && watchId !== null) {
            connection.invoke("GoOffline", instructorId);
        }
    });
})();

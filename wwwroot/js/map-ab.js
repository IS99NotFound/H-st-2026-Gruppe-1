window.resqAbMap = {
    initialize(map, { onMapClick }) {
        let abMode = null;
        let markerA = null;
        let markerB = null;
        let abLine = null;
        let abButton = null;
        let abDistanceMarker = null;
        let abActive = false;

        function createAbIcon(letter) {
            return L.divIcon({
                className: '',
                html: '<div class="ab-marker"><span>' + letter + '</span></div>',
                iconSize: [28, 28],
                iconAnchor: [13, 34]
            });
        }

        function resetAb() {
            if (markerA) map.removeLayer(markerA);
            if (markerB) map.removeLayer(markerB);
            if (abLine) map.removeLayer(abLine);
            if (abDistanceMarker) map.removeLayer(abDistanceMarker);
            markerA = markerB = abLine = abDistanceMarker = null;
        }

        function distanceInKilometres(pointA, pointB) {
            const earthRadius = 6371;
            const latitudeDifference = (pointB.lat - pointA.lat) * Math.PI / 180;
            const longitudeDifference = (pointB.lng - pointA.lng) * Math.PI / 180;
            const firstLatitude = pointA.lat * Math.PI / 180;
            const secondLatitude = pointB.lat * Math.PI / 180;
            const haversine = Math.sin(latitudeDifference / 2) ** 2
                + Math.cos(firstLatitude) * Math.cos(secondLatitude)
                * Math.sin(longitudeDifference / 2) ** 2;

            return earthRadius * 2 * Math.atan2(Math.sqrt(haversine), Math.sqrt(1 - haversine));
        }

        function setAbMode(mode) {
            abMode = mode;
            map.getContainer().classList.toggle('placing', mode !== null);
            abButton?.classList.toggle('active', abActive);
        }

        const AbControl = L.Control.extend({
            options: { position: 'topleft' },
            onAdd: function () {
                const container = L.DomUtil.create('div', 'leaflet-bar leaflet-control');
                abButton = L.DomUtil.create('a', 'ab-button', container);
                abButton.href = '#';
                abButton.title = 'Marker punkt A og B';
                abButton.setAttribute('role', 'button');
                abButton.innerHTML = '<span aria-hidden="true">A-B</span>';
                L.DomEvent.disableClickPropagation(container);
                L.DomEvent.on(abButton, 'click', function (event) {
                    L.DomEvent.preventDefault(event);
                    if (abActive) {
                        resetAb();
                        abActive = false;
                        setAbMode(null);
                    } else {
                        resetAb();
                        abActive = true;
                        setAbMode('A');
                    }
                });
                return container;
            }
        });

        new AbControl().addTo(map);
        map.on('click', event => {
            if (abActive && abMode === null) {
                resetAb();
                setAbMode('A');
            }
            if (abMode === 'A') {
                markerA = L.marker(event.latlng, { icon: createAbIcon('A') }).addTo(map);
                setAbMode('B');
                return;
            }
            if (abMode === 'B') {
                markerB = L.marker(event.latlng, { icon: createAbIcon('B') }).addTo(map);
                const pointA = markerA.getLatLng();
                const pointB = markerB.getLatLng();
                const distance = distanceInKilometres(pointA, pointB);
                abLine = L.polyline([pointA, pointB], {
                    color: '#fff', weight: 4, dashArray: '8 6'
                }).addTo(map);
                const midpoint = L.latLng(
                    (pointA.lat + pointB.lat) / 2,
                    (pointA.lng + pointB.lng) / 2
                );
                abDistanceMarker = L.marker(midpoint, {
                    icon: L.divIcon({
                        className: 'ab-distance-label',
                        html: `<span> ${distance.toFixed(2)} km </span>`,
                        iconSize: null
                    }),
                    interactive: false
                }).addTo(map);
                map.fitBounds(abLine.getBounds(), { padding: [60, 60] });
                setAbMode(null);
                return;
            }
            onMapClick(event);
        });
    }
};

window.resqRegistrationMap = {
    initialize({ form, mapElement, latitudeInput, longitudeInput, locationInput, positionText }) {
        if (!form || !mapElement || !latitudeInput || !longitudeInput || !window.L) return null;

        const map = L.map(mapElement).setView([58.4614, 8.7683], 10);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(map);

        let marker;
        const selectedPositionIcon = L.divIcon({
            className: 'resq-current-location-icon',
            html: '<i class="bi bi-geo-alt-fill" aria-hidden="true"></i>',
            iconSize: [30, 30],
            iconAnchor: [15, 30]
        });

        const setPosition = (lat, lng) => {
            latitudeInput.value = lat;
            longitudeInput.value = lng;
            marker?.remove();
            marker = L.marker([lat, lng], {
                icon: selectedPositionIcon
            }).addTo(map);

            const coordinates = `${lat.toFixed(5)}, ${lng.toFixed(5)}`;
            positionText.textContent = `Valgt sted: ${coordinates}`;
            window.resqReverseGeocode?.lookup(lat, lng).then(placeName => {
                if (!placeName) return;
                positionText.textContent = `Valgt sted: ${placeName} (${coordinates})`;
                if (locationInput) {
                    locationInput.value = placeName.replace(/^Nær\s+/, '');
                }
            });
        };

        map.on('click', event => setPosition(event.latlng.lat, event.latlng.lng));

        const LocateControl = L.Control.extend({
            options: { position: 'topright' },
            onAdd() {
                const container = L.DomUtil.create(
                    'div',
                    'leaflet-bar leaflet-control map-overlay-control');
                const button = L.DomUtil.create('a', 'registration-map-locate', container);
                button.href = '#';
                button.title = 'Vis min posisjon';
                button.setAttribute('role', 'button');
                button.setAttribute('aria-label', 'Vis min posisjon');
                button.innerHTML = '<i class="bi bi-crosshair" aria-hidden="true"></i>';
                L.DomEvent.disableClickPropagation(container);
                L.DomEvent.on(button, 'click', event => {
                    L.DomEvent.preventDefault(event);
                    if (!navigator.geolocation) {
                        positionText.textContent = 'Nåværende posisjon er ikke tilgjengelig.';
                        return;
                    }

                    navigator.geolocation.getCurrentPosition(position => {
                        setPosition(position.coords.latitude, position.coords.longitude);
                        map.setView([position.coords.latitude, position.coords.longitude], 13);
                    }, () => {
                        positionText.textContent = 'Kunne ikke hente nåværende posisjon.';
                    });
                });
                return container;
            }
        });
        new LocateControl().addTo(map);

        form.querySelector('#searchLocationButton')?.addEventListener('click', async () => {
            const input = form.querySelector('#locationNameInput');
            const button = form.querySelector('#searchLocationButton');
            if (!input?.value.trim() || !window.resqReverseGeocode) return;

            button.disabled = true;
            try {
                const result = await window.resqReverseGeocode.searchAddress(input.value.trim());
                if (!result) {
                    input.setCustomValidity('Fant ingen adresse.');
                    input.reportValidity();
                    return;
                }

                input.setCustomValidity('');
                input.value = result.formatted_query;
                setPosition(Number(result.lat), Number(result.lon));
                map.setView([Number(result.lat), Number(result.lon)], 14);
            } catch {
                input.setCustomValidity('Adressesøket kunne ikke gjennomføres.');
                input.reportValidity();
            } finally {
                button.disabled = false;
            }
        });

        form.querySelector('#useCurrentLocation')?.addEventListener('click', () => {
            map.getContainer().querySelector('.registration-map-locate')?.click();
        });

        return {
            map,
            setPosition,
            invalidateSize: () => map.invalidateSize()
        };
    }
};

// Helpers for registering a need or a resource by clicking on the map.
window.resqMapRegistration = {
    // Returns a function that shows a popup with the register buttons where the user clicked.
    bind(map, registrationUrls) {
        // The place names come from OpenStreetMap, also when another map layer is shown
        map.attributionControl?.addAttribution('Stedsnavn &copy; OpenStreetMap');

        return function showRegistrationChoices(latlng) {
            const content = L.DomUtil.create('div', 'map-register-popup');
            const heading = L.DomUtil.create('strong', 'map-register-popup-heading', content);
            heading.textContent = 'Valgt posisjon';

            const coordinates = L.DomUtil.create('span', 'map-register-popup-coordinates', content);
            coordinates.textContent = `Koordinater: ${latlng.lat.toFixed(5)}, ${latlng.lng.toFixed(5)}`;

            const actions = [
                { type: 'need', label: 'Registrer behov her', icon: 'bi-clipboard-plus' },
                { type: 'resource', label: 'Registrer ressurs her', icon: 'bi-box-seam' }
            ];

            for (const action of actions) {
                if (!registrationUrls[action.type]) continue;
                const link = L.DomUtil.create('a', 'btn btn-primary btn-sm map-register-action', content);
                const url = new URL(registrationUrls[action.type], document.baseURI);
                url.searchParams.set('register', action.type);
                url.searchParams.set('lat', latlng.lat.toString());
                url.searchParams.set('lng', latlng.lng.toString());
                link.href = url.toString();
                const icon = L.DomUtil.create('i', `bi ${action.icon}`, link);
                icon.setAttribute('aria-hidden', 'true');
                const label = L.DomUtil.create('span', '', link);
                label.textContent = action.label;
            }

            L.DomEvent.disableClickPropagation(content);
            const popup = L.popup({ maxWidth: 280, minWidth: 220 })
                .setLatLng(latlng)
                .setContent(content)
                .openOn(map);

            window.resqReverseGeocode.lookup(latlng.lat, latlng.lng).then(placeName => {
                if (!placeName) return;
                const address = L.DomUtil.create('span', 'map-register-popup-address');
                address.textContent = placeName;
                content.insertBefore(address, coordinates);
 
                if (popup.isOpen()) popup.update();
            });
        };
    },

    // Reads ?register=...&lat=...&lng=... from the address.
    // Gives null if something is missing or not a valid position.
    readRequestedLocation() {
        const query = new URLSearchParams(window.location.search);
        if (!query.has('register') || !query.has('lat') || !query.has('lng')) return null;

        const lat = Number(query.get('lat'));
        const lng = Number(query.get('lng'));
        if (!Number.isFinite(lat) || lat < -90 || lat > 90 ||
            !Number.isFinite(lng) || lng < -180 || lng > 180) return null;

        return { lat, lng };
    },

    // Returns a function that puts a marker on the map
    // and saves the position in the form fields.
    bindSelectedPosition(map, { latInput, lngInput, positionText, positionError, popupText, locationInput }) {
        let selectedMarker = null;

        return function setSelectedPosition(lat, lng, options = {}) {
            if (selectedMarker) {
                map.removeLayer(selectedMarker);
            }

            const coordinates = 'bredde: ' + lat.toFixed(5) + ', lengde: ' + lng.toFixed(5);

            const popupContent = document.createElement('span');
            popupContent.textContent = popupText + ': ' + lat.toFixed(5) + ', ' + lng.toFixed(5);

            const marker = L.marker([lat, lng]).addTo(map)
                .bindPopup(popupContent)
                .openPopup();
            selectedMarker = marker;

            latInput.value = lat;
            lngInput.value = lng;
            positionText.textContent = 'Valgt posisjon: ' + coordinates;
            positionError.textContent = '';

            window.resqReverseGeocode.lookup(lat, lng).then(placeName => {
                if (!placeName || marker !== selectedMarker) return;
                popupContent.textContent = placeName;
                marker.getPopup()?.update();
                positionText.textContent = 'Valgt posisjon: ' + placeName + ' (' + coordinates + ')';
                if (locationInput && !options.preserveLocationName) {
                    locationInput.value = placeName.replace(/^Nær\s+/, '');
                }
            });
        };
    }
};

// Opens the registration form (a Bootstrap modal) when a popup button asks for it.
document.addEventListener('resq:open-registration-modal', event => {
    const modal = document.querySelector('[data-registration-modal]');
    if (!modal || event.detail?.type !== modal.closest('body')?.querySelector('[data-registration-type]')?.dataset.registrationType) {
        return;
    }
    bootstrap.Modal.getOrCreateInstance(modal).show();
    event.detail.trigger?.blur();
});

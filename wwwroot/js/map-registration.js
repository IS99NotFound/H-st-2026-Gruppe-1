window.resqMapRegistration = {
    bind(map, registrationUrls) {
        return function showRegistrationChoices(latlng) {
            const content = L.DomUtil.create('div', 'map-register-popup');
            const heading = L.DomUtil.create('strong', 'map-register-popup-heading', content);
            heading.textContent = 'GPS fix';

            const coordinates = L.DomUtil.create('span', 'map-register-popup-coordinates', content);
            coordinates.textContent = `${latlng.lat.toFixed(5)}, ${latlng.lng.toFixed(5)}`;

            const actions = [
                { type: 'need', label: 'Register need here', icon: 'bi-clipboard-plus' },
                { type: 'resource', label: 'Deploy resource here', icon: 'bi-box-seam' }
            ];

            for (const action of actions) {
                if (!registrationUrls[action.type]) continue;
                const link = L.DomUtil.create('a', 'btn btn-primary btn-sm map-register-action', content);
                const url = new URL(registrationUrls[action.type], document.baseURI);
                url.searchParams.set('register', action.type);
                url.searchParams.set('lat', latlng.lat.toString());
                url.searchParams.set('lng', latlng.lng.toString());
                link.href = url.toString();
                if (action.type === registrationUrls.currentType) {
                    link.addEventListener('click', event => {
                        event.preventDefault();
                        document.dispatchEvent(new CustomEvent('resq:open-registration-modal', {
                            detail: { type: action.type, trigger: link }
                        }));
                    });
                }
                const icon = L.DomUtil.create('i', `bi ${action.icon}`, link);
                icon.setAttribute('aria-hidden', 'true');
                const label = L.DomUtil.create('span', '', link);
                label.textContent = action.label;
            }

            L.DomEvent.disableClickPropagation(content);
            L.popup({ maxWidth: 280, minWidth: 220 })
                .setLatLng(latlng)
                .setContent(content)
                .openOn(map);
        };
    },

    readRequestedLocation() {
        const query = new URLSearchParams(window.location.search);
        if (!query.has('register') || !query.has('lat') || !query.has('lng')) return null;

        const lat = Number(query.get('lat'));
        const lng = Number(query.get('lng'));
        if (!Number.isFinite(lat) || lat < -90 || lat > 90 ||
            !Number.isFinite(lng) || lng < -180 || lng > 180) return null;

        return { lat, lng };
    }
};

document.addEventListener('resq:open-registration-modal', event => {
    const modal = document.querySelector('[data-registration-modal]');
    if (!modal || event.detail?.type !== modal.closest('body')?.querySelector('[data-registration-type]')?.dataset.registrationType) {
        return;
    }
    bootstrap.Modal.getOrCreateInstance(modal).show();
    event.detail.trigger?.blur();
});

(() => {
    const modal = document.querySelector('[data-incident-registration-modal]');
    const mapElement = document.getElementById('incident-registration-map');
    const mapSection = document.querySelector('[data-incident-map-section]');
    const form = document.getElementById('incidentRegistrationForm');

    if (!modal || !mapElement || !mapSection || !form) return;

    const registrationMap = window.resqRegistrationMap.initialize({
        form,
        mapElement,
        latitudeInput: document.getElementById('incident-registration-latitude'),
        longitudeInput: document.getElementById('incident-registration-longitude'),
        positionText: document.getElementById('incident-registration-map-status')
    });

    if (!registrationMap) return;

    modal.addEventListener('shown.bs.modal', () => registrationMap.invalidateSize());
    document.addEventListener('resq:open-incident-registration', event => {
        registrationMap.setPosition(event.detail.lat, event.detail.lng);
        mapSection.classList.add('incident-registration-map-hidden');
        bootstrap.Modal.getOrCreateInstance(modal).show();
        event.detail.trigger?.blur();
    });

    modal.addEventListener('hidden.bs.modal', () => {
        mapSection.classList.remove('incident-registration-map-hidden');
    });

    if (modal.dataset.openIncidentRegistrationModal === 'true' ||
        new URLSearchParams(window.location.search).get('register') === 'incident') {
        bootstrap.Modal.getOrCreateInstance(modal).show();
    }
})();

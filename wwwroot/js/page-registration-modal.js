(() => {
    const modal = document.getElementById('pageRegistrationModal');
    const body = document.getElementById('pageRegistrationBody');
    const status = document.getElementById('pageRegistrationStatus');
    if (!modal || !body || !status) return;

    const modalInstance = bootstrap.Modal.getOrCreateInstance(modal);

    document.querySelectorAll('[data-registration-type]').forEach(link => {
        if (link.tagName !== 'A') return;

        link.addEventListener('click', event => {
            event.preventDefault();
            const type = link.dataset.registrationType;
            const existingModal = document.querySelector('[data-registration-modal]');
            const formSelector = type === 'need' ? '#needForm' : '#resourceForm';

            if (Number(existingModal?.querySelector('[name="Id"]')?.value) > 0) {
                // The page's form is editing something, so it is removed and an empty one is loaded
                existingModal.remove();
            } else if (existingModal?.querySelector(formSelector)) {
                bootstrap.Modal.getOrCreateInstance(existingModal).show();
                return;
            }

            body.querySelectorAll('.registration-form').forEach(form => form.remove());
            status.hidden = true;
            document.getElementById('pageRegistrationModalTitle').textContent =
                type === 'need' ? 'Registrer behov' : 'Registrer ressurs';
            modalInstance.show();
            fetch(link.href, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(response => response.ok ? response.text() : Promise.reject())
                .then(html => {
                    status.hidden = true;
                    body.insertAdjacentHTML('afterbegin', html);
                    initializeRegistrationForm(type);
                })
                .catch(() => {
                    status.hidden = false;
                });
        });
    });

    modal.addEventListener('hidden.bs.modal', () => {
        body.querySelectorAll('.registration-form').forEach(form => form.remove());
        status.hidden = true;
    });

    function initializeRegistrationForm(type) {
        const form = body.querySelector('.registration-form');
        if (!form) return;

        window.resqRegistration.initializeDateTimeFields(form.id, type === 'need'
            ? [
                { dateId: 'requiredFromDate', timeId: 'requiredFromTime', valueId: 'requiredFromValue' },
                { dateId: 'deadlineDate', timeId: 'deadlineTime', valueId: 'deadlineValue',
                  rangeErrorId: 'needDateRangeError' }
            ]
            : [
                { dateId: 'availableFromDate', timeId: 'availableFromTime', valueId: 'availableFromValue' },
                { dateId: 'expiryDate', timeId: 'expirationTime', valueId: 'expirationValue',
                  rangeErrorId: 'resourceDateRangeError' }
            ]);

        const mapElement = document.createElement('div');
        mapElement.className = 'registration-form-map';
        mapElement.setAttribute('role', 'application');
        mapElement.setAttribute('aria-label', 'Velg sted på kartet');
        form.querySelector('.registration-location')?.prepend(mapElement);

        const registrationMap = window.resqRegistrationMap.initialize({
            form,
            mapElement,
            latitudeInput: form.querySelector('[name="Latitude"]'),
            longitudeInput: form.querySelector('[name="Longitude"]'),
            locationInput: form.querySelector('[name="LocationName"]'),
            positionText: form.querySelector('.registration-location .registration-help')
        });
        modal.addEventListener('shown.bs.modal', () => registrationMap?.invalidateSize(), { once: true });
    }
})();

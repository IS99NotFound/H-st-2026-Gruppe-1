// Finds an approximate place name for a map position with OpenStreetMap's Nominatim.
// Usage policy: https://operations.osmfoundation.org/policies/nominatim/
window.resqReverseGeocode = (() => {

    const enabled = true;
    const endpoint = 'https://nominatim.openstreetmap.org/reverse';

    const minimumMillisecondsBetweenRequests = 1000;
    const zoomLevel = '16';

    const cache = new Map();
    let earliestNextRequest = 0;
    let active = null;

    function formatAddress(address) {
        if (!address) return null;
        const place = address.city || address.town || address.village || address.hamlet ||
            address.suburb || address.municipality || address.county;
        const parts = [address.road, place].filter(Boolean);
        return parts.length > 0 ? 'Nær ' + parts.join(', ') : null;
    }

    async function fetchPlaceName(lat, lng, signal) {
        const delay = earliestNextRequest - Date.now();
        if (delay > 0) {
            await new Promise(resolve => setTimeout(resolve, delay));
        }
  
        signal.throwIfAborted();
        earliestNextRequest = Date.now() + minimumMillisecondsBetweenRequests;

        const url = new URL(endpoint);
        url.searchParams.set('format', 'jsonv2');
        url.searchParams.set('lat', lat.toString());
        url.searchParams.set('lon', lng.toString());
        url.searchParams.set('zoom', zoomLevel);
        url.searchParams.set('accept-language', 'no');

        const response = await fetch(url, { signal });
        if (!response.ok) throw new Error('Kunne ikke finne adresse. ' + response.status);
        return formatAddress((await response.json()).address);
    }


    function lookup(lat, lng) {
        if (!enabled) return Promise.resolve(null);

        const key = lat.toFixed(4) + ',' + lng.toFixed(4);
        if (active?.key === key) return active.promise;

        active?.controller.abort();
        active = null;
        if (cache.has(key)) return Promise.resolve(cache.get(key));

        const controller = new AbortController();
        const current = { key, controller };
        current.promise = fetchPlaceName(lat, lng, controller.signal)
            .then(placeName => {
                cache.set(key, placeName);
                return placeName;
            })
            // Coordinates are still kept even if it doesn't get an address
            .catch(() => null)
            .finally(() => {
                if (active === current) active = null;
            });
        active = current;
        return current.promise;
    }

    return { lookup };
})();

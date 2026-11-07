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

    function distanceInKilometres(first, second) {
        const latitudeDifference = (first.lat - second.lat) * Math.PI / 180;
        const longitudeDifference = (first.lon - second.lon) * Math.PI / 180;
        const latitudeOne = first.lat * Math.PI / 180;
        const latitudeTwo = second.lat * Math.PI / 180;
        const haversine = Math.sin(latitudeDifference / 2) ** 2
            + Math.cos(latitudeOne) * Math.cos(latitudeTwo)
            * Math.sin(longitudeDifference / 2) ** 2;
        return 6371 * 2 * Math.atan2(Math.sqrt(haversine), Math.sqrt(1 - haversine));
    }

    function getCurrentPosition() {
        return new Promise(resolve => {
            if (!navigator.geolocation) {
                resolve(null);
                return;
            }
            navigator.geolocation.getCurrentPosition(
                position => resolve({
                    lat: position.coords.latitude,
                    lon: position.coords.longitude
                }),
                () => resolve(null),
                { enableHighAccuracy: false, timeout: 1500, maximumAge: 300000 });
        });
    }

    function formatSearchResult(query, result) {
        const address = result.address || {};
        const place = address.city || address.town || address.village ||
            address.hamlet || address.municipality;
        const region = address.state || address.county;
        const lowerQuery = query.toLocaleLowerCase('nb-NO');
        const additions = [place, region].filter(value =>
            value && !lowerQuery.includes(value.toLocaleLowerCase('nb-NO')));
        return additions.length > 0 ? `${query}, ${additions.join(', ')}` : query;
    }

    async function searchAddress(query) {
        const delay = earliestNextRequest - Date.now();
        if (delay > 0) {
            await new Promise(resolve => setTimeout(resolve, delay));
        }

        earliestNextRequest = Date.now() + minimumMillisecondsBetweenRequests;
        const url = new URL('https://nominatim.openstreetmap.org/search');
        url.searchParams.set('format', 'jsonv2');
        url.searchParams.set('q', query);
        url.searchParams.set('limit', '5');
        url.searchParams.set('accept-language', 'no');

        const response = await fetch(url);
        if (!response.ok) throw new Error('Kunne ikke søke etter adressen.');
        const results = await response.json();
        if (!results.length) return null;

        const currentPosition = await getCurrentPosition();
        if (currentPosition) {
            results.sort((first, second) =>
                distanceInKilometres(
                    { lat: Number(first.lat), lon: Number(first.lon) },
                    currentPosition)
                - distanceInKilometres(
                    { lat: Number(second.lat), lon: Number(second.lon) },
                    currentPosition));
        }

        const result = results[0];
        return {
            ...result,
            formatted_query: formatSearchResult(query, result)
        };
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

    return { lookup, searchAddress };
})();

document.addEventListener('keydown', event => {
    if (event.key !== 'Enter' || event.target?.id !== 'locationNameInput') return;

    event.preventDefault();
    document.getElementById('searchLocationButton')?.click();
});

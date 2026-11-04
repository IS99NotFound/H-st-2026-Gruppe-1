window.resqMapBase = {
    // Oppretter felles kart, standardlag og lagvelger for begge kart-sidene.
    initialize({ center, zoom }) {
        // OpenStreetMap brukes som standard. De andre lagene lastes først når brukeren velger dem.
        const openStreetMap = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap contributors'
        });
        const norgeskartLayer = L.tileLayer('https://cache.kartverket.no/v1/wmts/1.0.0/topo/default/webmercator/{z}/{y}/{x}.png', {
            attribution: '&copy; Kartverket'
        });
        const aerialPhotoLayer = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
            attribution: '&copy; Esri, Maxar, Earthstar Geographics'
        });
        const map = L.map('map', {
            center,
            zoom,
            layers: [openStreetMap]
        });

        // Bruker samme stil som de andre kartoverleggene på zoomkontrollen.
        map.zoomControl.getContainer().classList.add('map-overlay-control');

        return {
            map,
            // Legger til Layers-kontrollen etter at siden har lagt til A-B-kontrollen.
            addLayersControl() {
                const layersControl = L.control.layers({
                    "Standard": openStreetMap,
                    "Topografisk": norgeskartLayer,
                    "Flyfoto": aerialPhotoLayer
                }, null, {
                    position: 'topleft'
                }).addTo(map);
                layersControl.getContainer().classList.add(
                    'layers-menu-control',
                    'map-overlay-control'
                );
            }
        };
    }
};

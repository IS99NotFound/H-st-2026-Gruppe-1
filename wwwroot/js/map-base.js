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
        let currentLocationMarker = null;

        const locateCurrentPosition = () => {
            map.locate({
                setView: true,
                maxZoom: 13,
                enableHighAccuracy: false,
                timeout: 10000,
                maximumAge: 30000
            });
        };
        map.resqLocateCurrentPosition = locateCurrentPosition;

        map.on('locationfound', event => {
            if (currentLocationMarker) {
                currentLocationMarker.setLatLng(event.latlng);
            } else {
                currentLocationMarker = L.circleMarker(event.latlng, {
                    radius: 8,
                    color: '#ffffff',
                    weight: 2,
                    fillColor: '#258cfb',
                    fillOpacity: 1
                }).bindTooltip('Nåværende posisjon').addTo(map);
            }
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
            },
            addLocateControl() {
                const LocateControl = L.Control.extend({
                    options: { position: 'topleft' },
                    onAdd() {
                        const container = L.DomUtil.create(
                            'div',
                            'leaflet-bar leaflet-control map-overlay-control'
                        );
                        const button = L.DomUtil.create('a', 'locate-button', container);
                        button.href = '#';
                        button.title = 'Vis min posisjon';
                        button.setAttribute('role', 'button');
                        button.setAttribute('aria-label', 'Vis min posisjon');
                        button.innerHTML = '<i class="bi bi-crosshair" aria-hidden="true"></i>';
                        L.DomEvent.disableClickPropagation(container);
                        L.DomEvent.on(button, 'click', event => {
                            L.DomEvent.preventDefault(event);
                            locateCurrentPosition();
                        });
                        return container;
                    }
                });

                new LocateControl().addTo(map);
            },
            locateCurrentPosition() {
                locateCurrentPosition();
            }
        };
    }
};

(function () {
    const map = window.resqMap;

    if (!map) {
        console.error("The Resource Provider map was not found.");
        return;
    }

    const status = document.getElementById(
        "survival-filter-status"
    );

    const markers = L.layerGroup().addTo(map);

    const categoryLabels = {
        hospitals: "Hosptial",
        drinking_water: "Drinking water",
        shelter: "Shelter / bunker"
    };

    let locations = [];
    let requestController = null;
    let moveTimer = null;

    function getCategory(tags) {
        if (tags.amenity === "hospital") {
            return "hospital";
        }
        if (tags.amenity === "drinking_water"){
            return "drinking_water";
        }
        if (
            tags.amenity === "shelter" ||
            tags.emergency === "shelter" ||
            tags.emergency === "assembly_point" ||
            tags.military === "bunker"
        ){
            return "shelter";
        }
   
        return null;
    }

    async function loadLocations(){
        if (requestController) {
            requestController.abort();
        }

        requestController = new AbortController();

        const bounds = map.getBounds();
        const bbox = [
            bounds.getSouth(),
            bounds.getWest(),
            bounds.getNorth(),
            bounds.getEast()
        ].join(",");

        const query = `
            [out:json][timeout:25];
            (
                nwr["amenity"="hospital"](${bbox});
                nwr["amenity"="drinking_water"](${bbox});
                nwr[amenity"="shelter"](${bbox});
                nwr["emergency"="shelter](${bbox});
                nwr["emergency"="assembly_point"](${bbox});
                nwr["military"="bunker"](${bbox});
            );
            out center tags;
            `;

            status.textContent = "Searching for existing locations...";
            //Website som allerede har survival locations//
            try {
                const response = await fetch(
                    "https://overpass-api.de/api/interpreter",
                    {
                        method: "POST",
                        body: new URLSearchParams({ data: query}),
                        signal: requestController.signal
                    }
                );

                if (!response.ok) {
                    throw new Error("The location service returned an error.");
                }

                const data = await response.json();

                locations = data.elements.map(place => {
                    const tags =place.tags || {};

                    return{
                        name: tags.name || "Unnamed location",
                        category: getCategory(tags),
                        latitude: place.lat ?? place.center?.lat,
                        longitude: place.lon ?? place.center?.lon,
                        address: [
                            tags["addr:street"],
                            tags["addr:housenumber"],
                            tags["addr:city"]
                        ].filter.[Boolean].json("")
                    };
                }).filter(place =>
                    place.category &&
                    Number.isFinite(place.latitude) &&
                    Number.isFinite(place.longitude)
                );

                renderMarkers();

                status.textContent =
                `${locations.length} existing locations found in this map area.`;
            }catch (error) {
                if (error.name === "AbortError"){
                    return;
                }

                console.error(error);
                status.textContent =
                    "Could not load locations. Try again later.";
            }
        }

        function renderMarkers() {
            markers.clearLayers();

            const selectedCategoriers = Array.from(
                document.querySelectorAll(".survival-filter:checked")
            )
        }
    }
}
}
}
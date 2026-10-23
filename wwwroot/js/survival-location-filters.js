(function () {
    const map = window.resqMap;

    if (!map) {
        console.error("The Resource Provider map was not found.");
        return;
    }

    const status = document.getElementById("survival-filter-status");
    const markers = L.layerGroup().addTo(map);

    const categoryLabels = {
        hospital: "Hospital",
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
   
        function renderMarkers() {
            markers.clearLayers();

            const selectedCategories = Array.from(
                document.querySelectorAll(".survival-filter:checked")
            ).map(input => input.value);

            let visibleCount = 0;

            locations.forEach(place => {
                if (!selectedCategories.includes(place.category)) {
                    return;
                }
                
                //Lager en popup for kart-markering, legger til location name og category
                const popup= document.createElement('div');
                const title = document.createElement("strong");

                title.textContent = place.name;
                popup.appendChild(title);
                popup.appendChild(document.createElement("br"));

                const category = document.createElement("span")
                category.textContent =
                    categoryLabels[place.category] || place.category;

                popup.appendChild(category);
             
                if (place.address) {
                    popup.appendChild(document.createElement("br"));

                    const address = document.createElement("span");
                    address.textContent = place.address;
                    
                    popup.appendChild(address);
                }

                L.marker([place.latitude, place.longitude])
                .bindPopup(popup)
                .addTo(markers);

                visibleCount++;
            });

            if (status) {
                status.textContent =
                `${visibleCount} nearby locations shown on the map.`;
            }
        }

    async function loadLocations(){
        if (requestController) {
            requestController.abort();
        }

        const controller = new AbortController();
        requestController = controller;

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
                nwr["amenity"="shelter"](${bbox});
                nwr["emergency"="shelter"](${bbox});
                nwr["emergency"="assembly_point"](${bbox});
                nwr["military"="bunker"](${bbox});
            );
            out center tags;
            `;

            if (status) {
                status.textContent = "Searching for nearby locations..."
            }
          
            //Website som allerede har survival locations//
            try {
                const response = await fetch(
                    "https://overpass-api.de/api/interpreter",
                    {
                        method: "POST",
                        body: new URLSearchParams({ data: query}),
                        signal: controller.signal
                    }
                );

                if (!response.ok) {
                    throw new Error(`Overpass returned HTTP ${response.status}`);
                }

                const data = await response.json();

                if(!Array.isArray(data.elements)) {
                    throw new Error("Unexpected response from application.");
                }

                //Linjer og relasjoner kan ha koordinater angitt som et senterpunkt.//
                locations = data.elements.map(place => {
                    const tags =place.tags || {};

                    const latitude = place.lat ?? place.center?.lat;
                    const longitude = place.lon ?? place.center?.lon;

                    return{
                        name: tags.name || "Unnamed location",
                        category: getCategory(tags),
                        latitude,
                        longitude,
                        address: [
                            tags["addr:street"],
                            tags["addr:housenumber"],
                            tags["addr:city"]
                        ].filter(Boolean).join("")
                    };
                })
                .filter(place =>
                    place.category &&
                    Number.isFinite(place.latitude) &&
                    Number.isFinite(place.longitude)
                );

                renderMarkers();

            }  catch (error) {
                if (error.name === "AbortError"){
                    return;
                }

                console.error("Could not load locations.", error);

                if (status) {
                    status.textContent = "Could not load nearby locations. Pleasy try again later";
                }
            }
        }

    document.querySelectorAll(".survival-filter")
        .forEach(input => {
            input.addEventListener("change", renderMarkers);
        });

    map.on("moveend", function (){
        clearTimeout(moveTimer);
        moveTimer = setTimeout(loadLocations, 500);  
    });
    
    //Load locations for kartet
    loadLocations();
})();

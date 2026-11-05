(function () {
    const map = window.resqMap;

    if (!map) {
        console.error("The Public Actor map was not found.");
        return;
    }

    //Gets the information on the mapsHTML-element
    const mapElement = document. getElementById("map");

     // URL for GetResourceMarkers() in PublicActorController.
    const resourcesUrl = mapElement?.dataset.resourceMarkersUrl;

    // Statusfield under mapfiltering.
    const statusElement = document.getElementById(
        "registered-resources-status"
    );

    // Checkbox for showing unavailable resources
    const unavailableCheckbox = document.getElementById(
        "show-unavailable-resources"
    );


    //Starts with resources that already is fetched from the server
    let resources = Array.isArray(window.publicActorResources)
        ? window.publicActorResources
        : [];

    
    const resourceMarkers = L.layerGroup().addTo(map);

    //Is used to avoid old requests from rewriting newer data.
    let activeController = null;
    let latestRequestId = 0;

    function getSelectedTypes() {
        return Array.from(
            document.querySelectorAll(
                ".registered-resources-filter:checked"
            )
        ).map(input => input.value);
    }

    //Makes status readable
    function readableStatus(status) {
        switch ((status || "").toLowerCase()) {
            case "available":
                return "Tilgjengelig";

            case "unavailable":
                return "Tildelt / utilgjengelig";

            default:
                return status || "Ukjent status";
        }
    }

    //Shows the resurces on the map
    function renderResources() {
        
        resourceMarkers.clearLayers();

        //Gets the categories that the user choose
        const selectedTypes = getSelectedTypes();
        
        let visibleCount = 0;

        resources.forEach(resource => {
            const latitude = Number(resource.latitude);
            const longitude = Number(resource.longitude);

            const resourceStatus = (
                resource.status || "available"
            ).toLowerCase()


            //Ignores resources that do not match the filters
            if (!selectedTypes.includes(resource.resourceType)) {
                return;
            }

            //Checks for valid coordinates
            if (
                resource.latitude == null ||
                resource.longitude == null ||
                !Number.isFinite(latitude) ||
                !Number.isFinite(longitude) ||
        //Sjekker om koordinatene er innen en gyldig "range" for ressurser//
                latitude < -90 ||
                latitude > 90 ||
                longitude < -180 ||
                longitude > 180
            ){
                return;
            }

            //Unavailable resource will never be shown 
            //Unless a user has activated it
            if (
                resourceStatus === "unavailable" &&
                !unavailableCheckbox?.checked
            ) {
                return;
            }

            //Do not show unknown status-resources
            if (
                resourceStatus !== "available" &&
                resourceStatus !== "unavailable"
            ){
                return;
            }

            //Checks if the marker is outside of map-bounds
            if(!map.getBounds().contains([latitude, longitude])) {
                return;
            }
        
            //Colours based on resource status
            const markerClass = resourceStatus === "available"
                ? "available-resource"
                : "unavailable-resource";

            //Makes map-pins
            const markerIcon = L.divIcon({
                className: "resq-map-marker-icon",
                html: `<span class="resq-map-marker resq-map-marker--${markerClass}"></span>`,
                iconSize: [18, 18],
                iconAnchor: [9, 9]
            });
            const marker = L.marker([latitude, longitude], { icon: markerIcon });


            //Creating a popup-window for resource
            const popup = document.createElement("div");
            const title = document.createElement("strong");

            title.textContent = resource.name || "Ressurs uten navn";
            popup.appendChild(title);
            popup.appendChild(document.createElement("br"));

            const type = document.createElement("span");
            type.textContent = resource.resourceType || "Annet";
            popup.appendChild(type);




            popup.appendChild(document.createElement("br"));

            const statusText = document.createElement("span");

            statusText.textContent =
                "Status: " + readableStatus(resourceStatus);
            popup.appendChild(statusText);
            

        
            if (resource.description) {

                popup.appendChild(document.createElement("br"));

                const description = document.createElement("span");

                description.textContent = resource.description;

                popup.appendChild(description);
            }

            if (resource.phone) {
                popup.appendChild(document.createElement("br"));

                const contact = document.createElement("span");
                contact.textContent = "Kontakt: " + resource.phone;
                popup.appendChild(contact);
            }

            marker.bindPopup(popup);
            marker.addTo(resourceMarkers);

            visibleCount ++;
        });
        
    if(statusElement) {
        statusElement.textContent = 
        `${visibleCount} registrerte ressurser vist på kartet.`;
    }
}

    async function refreshResources() {
        if(!resourcesUrl) {
            console.error(
                "URL til GetResourceMarkers mangler på kartet."
            );

            return;
        }

        //Will cancel potensial older requests
        if (activeController) {
            activeController.abort();
        }

        const controller = new AbortController();
        activeController = controller;
        const requestId = ++latestRequestId;

         const includeUnavailable = Boolean(
            unavailableCheckbox?.checked
        );

        const url = new URL(
            resourcesUrl,
            window.location.origin
        );
        url.searchParams.set(
            "includeUnavailable",
            String(includeUnavailable)
        );

        try {
            //Will fetch fresh data from PublicActorController.
            const response =await fetch(url.toString(), {
                method: "GET",
                headers: {
                    "Accept": "application/json"
                },

                cache: "no-store",
                signal: controller.signal
        });

        // Control to see if the server responded normally.
        if (!response.ok) {

            throw new Error(
                `Serveren svarte med HTTP ${response.status}.`
            );
        }


        const latestResources = await response.json();

        if(!Array.isArray(latestResources)) {
            throw new Error(
                "Serveren returnerte ugyldige ressursdata."
            );
        }

        if (requestId !== latestRequestId) {
                return;
            }

        //Updates the resource-list with the newest of data.
        resources = latestResources;

        window.publicActorResources =latestResources;

        renderResources();

    }catch (error){
        //Cancelled requests are expected when filtering changes fast.
        if (error.name === "AbortError") {
            return;
        }

        console.error(
            "Kunne ikke hente oppdaterte ressurser:",
            error
        );

        if (statusElement) {
            statusElement.textContent = 
                "Kunne ikke oppdatere ressursene. Viser sist hentede data.";
        }
    }
}


    document.querySelectorAll(".registered-resources-filter")
        .forEach(input => {
            input.addEventListener("change", renderResources);
        });

    if (unavailableCheckbox) {

        unavailableCheckbox.addEventListener(
            "change",
            refreshResources
        );
    }

    map.on("moveend", renderResources);
    
    renderResources();
    refreshResources();

    // Fetch fresh data when user is back in application
    document.addEventListener("visibilitychange", () => {

        if (!document.hidden) {

            refreshResources();

        }

    });
})();
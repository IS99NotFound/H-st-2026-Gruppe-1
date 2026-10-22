(function () {
    const map = window.resqMap;

    if (!map) {
        console.error("The Public Actor map was not found.");
        return;
    }

    //Hent registrerte ressurser, eller bruk en tom liste, om ingen finnes//
    const resources = Array.isArray(window.publicActorResource)
        ? window.publicActorResources
        : [];

    const status = document.getElementById(
        "registered-resources-status"
    );

    const resourceMarkers = L.layerGroup().addTo(map);

    function renderResources() {
        resourceMarkers.clearLayers();

        const selectedTypes = Array.from(
            document.querySelectorAll(
                ".registered-resources-filte:checked"
            )
        ).map(input => input.value);

        let visibleCount = 0;

        resources.forEach(resource => {
            if (
                !selectedTypes.includes(resource.resourceType) ||
                !selectedTypes.includes(resource.resourceType) ||
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


            if(!map.getBounds().contains([latitude, longitude])) {
                return;
            }
        
            const marker = L.marker([latitude, longitude]);

            const popup = document.createElement("div");
            const title = document.createElement("strong");

            title.textContent = resource.name || "Unnamed resource";
            popup.appendChild(title);
            popup.appendChild(document.createElement("br"));

            const type = document.createElement("span");
            type.textContent = resource.resourceType || "Other";
            popup.appendChild(type);

            if (resource.description) {
                popup.appendChild(document.createElement("br"));

                const description = document.createElement("span");
                description.textContent = resource.description;
                popup.appendChild(description);
            }

            if (resource.phone) {
                popup.appendChild(document.createElement("br"));

                const contact = document.createElement("span");
                contact.textContent = "Contact: " + resource.phone;
                popup.appendChild(contact);
            }

            marker.bindPopup(popup);
            marker.addTo(resourceMarkers);

            visibleCount ++;
        });
        
    if(status) {
        status.textContent = 
        `${visibleCount} registered resources shown on the map.`;
    }
}

    document.querySelectorAll(".registered-resource-filter")
        .forEach(input => {
            input.addEventListener("change", renderResources);
        });

    map.on("moveend", renderResources);
    
    renderResources();
})();
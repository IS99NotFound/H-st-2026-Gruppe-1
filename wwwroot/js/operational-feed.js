(function () {
    const list = document.getElementById("operational-feed-list");
    const search = document.getElementById("feed-search");
    const radius = document.getElementById("feed-radius");
    const locationStatus = document.getElementById("feed-location-status");
    const radiusValue = document.getElementById("feed-radius-value");
    const resizer = document.querySelector(".feed-resizer");
    const empty = document.querySelector(".feed-empty");
    const currentLocationButton = document.querySelector("[data-feed-current-location]");
    const resetLocationButton = document.querySelector("[data-feed-reset-location]");
    const filterToggle = document.querySelector("[data-feed-filter-toggle]");
    const filterControls = document.getElementById("feed-filter-controls");
    const resultsToggle = document.querySelector("[data-feed-results-toggle]");
    const resultsLabel = document.querySelector("[data-feed-results-label]");
    const items = Array.from(document.querySelectorAll("[data-feed-item]"));
    let activeType = "all";
    let currentLocation = null;
    let currentLocationMarker = null;
    let focusedFeedMarker = null;

    if (!list || !search || !radius) return;

    function addIncidentMarkers() {
        const map = window.resqMap;
        const incidents = Array.isArray(window.operationalFeedIncidents)
            ? window.operationalFeedIncidents
            : [];
        if (!map || !incidents.length) return;

        const incidentMarkers = L.layerGroup().addTo(map);
        incidents.forEach(incident => {
            const latitude = Number(incident.latitude);
            const longitude = Number(incident.longitude);
            if (!Number.isFinite(latitude) || !Number.isFinite(longitude) ||
                latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180) {
                return;
            }

            const popup = document.createElement("div");
            const title = document.createElement("strong");
            title.textContent = incident.title || "Incident";
            popup.appendChild(title);
            if (incident.description) {
                popup.appendChild(document.createElement("br"));
                const description = document.createElement("span");
                description.textContent = incident.description;
                popup.appendChild(description);
            }

            L.marker([latitude, longitude])
                .bindPopup(popup)
                .addTo(incidentMarkers);
        });
    }

    function distanceInKm(latitude, longitude) {
        if (!currentLocation) return Number.POSITIVE_INFINITY;
        const earthRadius = 6371;
        const latDistance = (latitude - currentLocation.latitude) * Math.PI / 180;
        const lngDistance = (longitude - currentLocation.longitude) * Math.PI / 180;
        const a = Math.sin(latDistance / 2) ** 2 +
            Math.cos(currentLocation.latitude * Math.PI / 180) *
            Math.cos(latitude * Math.PI / 180) * Math.sin(lngDistance / 2) ** 2;
        return earthRadius * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    }

    function showCurrentLocation() {
        const map = window.resqMap;
        if (!map || !currentLocation) return;

        const location = [currentLocation.latitude, currentLocation.longitude];
        if (currentLocationMarker) {
            currentLocationMarker.setLatLng(location);
        } else {
            currentLocationMarker = L.circleMarker(location, {
                radius: 8,
                color: "#ffffff",
                weight: 2,
                fillColor: "#258cfb",
                fillOpacity: 1
            }).bindTooltip("Current location").addTo(map);
        }
        map.setView(location, 13);
    }

    function requestCurrentLocation() {
        if (!navigator.geolocation) {
            locationStatus.textContent = "Current location is not available in this browser.";
            return;
        }

        if (currentLocationButton) currentLocationButton.disabled = true;
        locationStatus.textContent = "Finding your current location...";
        navigator.geolocation.getCurrentPosition(position => {
            currentLocation = {
                latitude: position.coords.latitude,
                longitude: position.coords.longitude
            };
            showCurrentLocation();
            render();
            if (currentLocationButton) currentLocationButton.disabled = false;
        }, () => {
            locationStatus.textContent = "Could not access your current location.";
            render();
            if (currentLocationButton) currentLocationButton.disabled = false;
        });
    }

    function focusFeedItem(item) {
        const map = window.resqMap;
        if (!map) return;
        const lat = Number(item.dataset.latitude);
        const lng = Number(item.dataset.longitude);
        if (!Number.isFinite(lat) || !Number.isFinite(lng)) return;

        if (focusedFeedMarker) map.removeLayer(focusedFeedMarker);
        focusedFeedMarker = L.circleMarker([lat, lng], {
            radius: 10,
            color: "#ffffff",
            weight: 3,
            fillColor: "#4a9eff",
            fillOpacity: 0.9
        }).addTo(map);
        map.setView([lat, lng], 14);
        focusedFeedMarker.bindPopup(item.dataset.feedTitle || "Selected location").openPopup();
    }

    function render() {
        const query = search.value.trim().toLowerCase();
        const maxRadius = Number(radius.value);
        radiusValue.textContent = maxRadius ? `${maxRadius} km` : "All distances";
        let visible = 0;
        const sortedItems = items
            .map(item => ({ item, distance: distanceInKm(Number(item.dataset.latitude), Number(item.dataset.longitude)) }))
            .sort((a, b) => a.distance - b.distance);

        sortedItems.forEach(({ item, distance }) => {
            list.appendChild(item);
            const matchesType = activeType === "all" || item.dataset.feedType === activeType;
            const matchesSearch = !query || item.dataset.feedSearch.toLowerCase().includes(query);
            const matchesRadius = !maxRadius || !currentLocation || distance <= maxRadius;
            const isVisible = matchesType && matchesSearch && matchesRadius;
            item.hidden = !isVisible;
            if (isVisible) visible++;
        });
        empty.hidden = visible !== 0;
        if (resultsLabel) {
            const resultText = `${visible} result${visible === 1 ? "" : "s"}`;
            resultsLabel.textContent = currentLocation
                ? `Show ${resultText}, sorted by distance from your location`
                : `Show ${resultText}`;
        }
        if (currentLocation) {
            locationStatus.textContent = `${visible} items shown, sorted by distance from your location.`;
        } else {
            locationStatus.textContent = `${visible} items shown. Allow location to sort and filter by distance.`;
        }
    }

    document.querySelectorAll(".operational-feed-tabs [data-feed-type]").forEach(button => {
        button.addEventListener("click", () => {
            activeType = button.dataset.feedType;
            document.querySelectorAll(".operational-feed-tabs [data-feed-type]").forEach(tab => {
                const active = tab === button;
                tab.classList.toggle("active", active);
                tab.classList.toggle("btn-primary", active);
                tab.classList.toggle("btn-outline-secondary", !active);
            });
            render();
        });
    });

    filterToggle?.addEventListener("click", () => {
        const isOpen = filterControls?.classList.toggle("is-open") ?? false;
        filterToggle.setAttribute("aria-expanded", String(isOpen));
    });

    resultsToggle?.addEventListener("click", () => {
        const isOpen = list.classList.toggle("is-open");
        resultsToggle.setAttribute("aria-expanded", String(isOpen));
        resultsToggle.classList.toggle("is-open", isOpen);
    });
    search.addEventListener("input", render);
    radius.addEventListener("change", render);
    radius.addEventListener("input", render);
    items.forEach(item => {
        item.addEventListener("click", event => {
            if (event.target.closest("a, button")) return;
            focusFeedItem(item);
        });
        item.addEventListener("keydown", event => {
            if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                focusFeedItem(item);
            }
        });
    });

    if (currentLocationButton) {
        currentLocationButton.addEventListener("click", requestCurrentLocation);
    }
    if (resetLocationButton) {
        resetLocationButton.addEventListener("click", () => {
            search.value = "";
            radius.value = "0";
            activeType = "all";
            document.querySelectorAll(".operational-feed-tabs [data-feed-type]").forEach(tab => {
                const active = tab.dataset.feedType === "all";
                tab.classList.toggle("active", active);
                tab.classList.toggle("btn-primary", active);
                tab.classList.toggle("btn-outline-secondary", !active);
            });
            if (focusedFeedMarker && window.resqMap) {
                window.resqMap.removeLayer(focusedFeedMarker);
                focusedFeedMarker = null;
                window.resqMap.closePopup();
            }
            if (currentLocation) {
                showCurrentLocation();
                render();
            } else {
                requestCurrentLocation();
            }
        });
    }

    if (resizer) {
        let resizing = false;
        const setFeedWidth = width => {
            const feed = document.querySelector(".operational-feed");
            if (!feed) return;
            const clampedWidth = Math.max(260, Math.min(520, width));
            document.documentElement.style.setProperty("--feed-width", `${clampedWidth}px`);
            window.resqMap?.invalidateSize();
        };
        const stopResizing = () => {
            resizing = false;
            document.body.classList.remove("is-resizing-feed");
        };
        resizer.addEventListener("pointerdown", event => {
            if (window.matchMedia("(max-width: 991px)").matches) return;
            resizing = true;
            resizer.setPointerCapture(event.pointerId);
            document.body.classList.add("is-resizing-feed");
            event.preventDefault();
        });
        resizer.addEventListener("pointermove", event => {
            if (resizing) setFeedWidth(event.clientX);
        });
        resizer.addEventListener("pointerup", stopResizing);
        resizer.addEventListener("pointercancel", stopResizing);
        resizer.addEventListener("keydown", event => {
            if (event.key === "ArrowLeft" || event.key === "ArrowRight") {
                const currentWidth = parseInt(getComputedStyle(document.querySelector(".operational-feed")).width, 10);
                setFeedWidth(currentWidth + (event.key === "ArrowRight" ? 16 : -16));
                event.preventDefault();
            }
        });
    }

    requestCurrentLocation();
    addIncidentMarkers();
    render();
})();

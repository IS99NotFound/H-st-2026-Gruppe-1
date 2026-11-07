// The live feed on the dashboard: search, filter, sort by distance, export, and show items on the map.
// Everything is inside a function, so the variables do not leak into the rest of the page.
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
    const exportToggle = document.querySelector("[data-feed-export-toggle]");
    const exportOptions = document.getElementById("feed-export-options");
    const exportButtons = document.querySelectorAll("[data-feed-export]");
    const exportBackButton = document.querySelector("[data-feed-export-back]");
    const resultsToggle = document.querySelector("[data-feed-results-toggle]");
    const items = Array.from(document.querySelectorAll("[data-feed-item]"));
    const feed = document.querySelector(".operational-feed");
    const mapArea = document.querySelector(".map-area");
    const mapLegendToggle = document.querySelector("[data-map-legend-toggle]");
    const mapLegendContent = document.querySelector(".map-color-legend-content");
    // State: the chosen tab (all, resource, need or incident), the position of the user, and map markers.
    let activeType = "all";
    let currentLocation = null;
    let currentLocationMarker = null;
    let focusedFeedMarker = null;

    if (!list || !search || !radius) return;

    function setCardHeight(item, faceSelector) {
        const inner = item.querySelector(".feed-card-inner");
        const face = item.querySelector(faceSelector);
        if (!inner || !face) return;
        inner.style.height = `${face.scrollHeight}px`;
    }

    items.forEach(item => setCardHeight(item, ".feed-card-front"));

    const closeMapLegend = () => {
        if (!mapLegendToggle || !mapLegendContent) return;

        mapLegendContent.classList.add("is-collapsed");
        mapLegendToggle.setAttribute("aria-expanded", "false");
        mapLegendToggle.querySelector("i")?.classList.remove("bi-chevron-down");
        mapLegendToggle.querySelector("i")?.classList.add("bi-chevron-up");
    };

    if (feed && mapArea && "ResizeObserver" in window) {
        const updateMapOverlayPosition = () => {
            mapArea.style.setProperty(
                "--operational-feed-height",
                `${feed.getBoundingClientRect().height}px`
            );
        };
        const feedResizeObserver = new ResizeObserver(updateMapOverlayPosition);
        feedResizeObserver.observe(feed);
        updateMapOverlayPosition();
    }

    function addFeedMarkers() {
        const map = window.resqMap;
        const feedItems = window.operationalFeedItems || {};
        const items = [
            ...(Array.isArray(feedItems.needs) ? feedItems.needs : []),
            ...(Array.isArray(feedItems.incidents) ? feedItems.incidents : [])
        ];
        if (!Array.isArray(window.publicActorResources)) {
            items.push(...(Array.isArray(feedItems.resources) ? feedItems.resources : []));
        }
        if (!map || !items.length) return;

        const feedMarkers = L.layerGroup().addTo(map);
        items.forEach(item => {
            const latitude = Number(item.latitude);
            const longitude = Number(item.longitude);
            if (!Number.isFinite(latitude) || !Number.isFinite(longitude) ||
                latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180) {
                return;
            }

            // Build the popup with textContent (not innerHTML), so text from the database cannot add HTML to the page.
            const popup = document.createElement("div");
            const title = document.createElement("strong");
            title.textContent = item.title || "Uten tittel";
            popup.appendChild(title);
            if (item.description) {
                popup.appendChild(document.createElement("br"));
                const description = document.createElement("span");
                description.textContent = item.description;
                popup.appendChild(description);
            }

            const colorKey = item.colorKey || "public-team";
            const markerIcon = L.divIcon({
                className: "resq-map-marker-icon",
                html: `<span class="resq-map-marker resq-map-marker--${colorKey}"></span>`,
                iconSize: [18, 18],
                iconAnchor: [9, 9]
            });

            L.marker([latitude, longitude], { icon: markerIcon })
                .bindPopup(popup)
                .addTo(feedMarkers);
        });
    }

    // Distance in km from the user's position to a point (haversine formula).
    // Gives infinity if we do not know where the user is.
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
            }).bindTooltip("Nåværende posisjon").addTo(map);
        }
        map.setView(location, 13);
    }

    function requestCurrentLocation() {
        if (!navigator.geolocation) {
            locationStatus.textContent = "Nåværende posisjon er ikke tilgjengelig i denne nettleseren.";
            return;
        }

        if (currentLocationButton) currentLocationButton.disabled = true;
        locationStatus.textContent = "Finner din nåværende posisjon...";
        navigator.geolocation.getCurrentPosition(position => {
            currentLocation = {
                latitude: position.coords.latitude,
                longitude: position.coords.longitude
            };
            showCurrentLocation();
            render();
            if (currentLocationButton) currentLocationButton.disabled = false;
        }, () => {
            locationStatus.textContent = "Fikk ikke tilgang til posisjonen din.";
            render();
            if (currentLocationButton) currentLocationButton.disabled = false;
        });
    }

    function focusFeedItem(item) {
        items.forEach(otherItem => {
            if (otherItem === item) return;
            otherItem.classList.remove("is-flipped");
            setCardHeight(otherItem, ".feed-card-front");
            otherItem.querySelector(".feed-card-back")?.setAttribute("aria-hidden", "true");
            otherItem.querySelector(".feed-card-front")?.setAttribute("aria-hidden", "false");
            otherItem.setAttribute("aria-label", `Vis ${otherItem.dataset.feedTitle || "element"} på kartet`);
        });
        const isFlipped = item.classList.toggle("is-flipped");
        setCardHeight(item, isFlipped ? ".feed-card-back" : ".feed-card-front");
        item.querySelector(".feed-card-back")?.setAttribute("aria-hidden", String(!isFlipped));
        item.querySelector(".feed-card-front")?.setAttribute("aria-hidden", String(isFlipped));
        item.setAttribute("aria-label", isFlipped
            ? `Skjul detaljer for ${item.dataset.feedTitle || "element"}`
            : `Vis ${item.dataset.feedTitle || "element"} på kartet`);

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
        focusedFeedMarker.bindPopup(item.dataset.feedTitle || "Valgt sted").openPopup();
    }

    // Puts a value in quotes for a CSV file. A quote inside the value becomes two quotes.
    function csvValue(value) {
        return `"${String(value ?? "").replaceAll('"', '""')}"`;
    }

    function getFilteredResources() {
        const maxRadius = Number(radius.value);
        return items
            .filter(item => item.dataset.feedType === "resource")
            .map(item => ({
                item,
                distance: distanceInKm(Number(item.dataset.latitude), Number(item.dataset.longitude))
            }))
            .filter(({ item, distance }) => {
                const hasCoordinates = Number.isFinite(Number(item.dataset.latitude)) &&
                    Number.isFinite(Number(item.dataset.longitude));
                return hasCoordinates && (!maxRadius || !currentLocation || distance <= maxRadius);
            });
    }

    function getResourceRows() {
        return [
            ["Navn", "Kategori", "Beskrivelse", "Status", "Breddegrad", "Lengdegrad", "Registrert dato", "Registrert klokkeslett"],
            ...getFilteredResources().map(({ item }) => {
                const [registeredDate = "", registeredTime = ""] =
                    (item.dataset.feedCreatedAt || "").split(" ");
                return [
                    item.dataset.feedTitle,
                    item.dataset.feedCategory,
                    item.dataset.feedDescription,
                    item.dataset.feedStatus,
                    `${item.dataset.latitude}°`,
                    `${item.dataset.longitude}°`,
                    registeredDate,
                    registeredTime
                ];
            })
        ];
    }

    function downloadFile(content, fileName, contentType) {
        const blob = new Blob([content], { type: contentType });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
    }

    function exportResources(format) {
        const resourceRows = getResourceRows();
        const resources = getFilteredResources().map(({ item }) => ({
            name: item.dataset.feedTitle,
            category: item.dataset.feedCategory,
            description: item.dataset.feedDescription,
            status: item.dataset.feedStatus,
            latitude: Number(item.dataset.latitude),
            longitude: Number(item.dataset.longitude),
            registered: item.dataset.feedCreatedAt
        }));

        if (format === "json") {
            downloadFile(
                JSON.stringify(resources, null, 2),
                "resources.json",
                "application/json;charset=utf-8"
            );
            return;
        }

        const csv = "\uFEFF" + resourceRows.map(row => row.map(csvValue).join(";")).join("\r\n");
        // CSV: the text starts with a BOM character, so Excel shows the Norwegian letters correctly.
        downloadFile(csv, "resources.csv", "text/csv;charset=utf-8");
    }

    // Updates the list: sorts by distance and hides items that do not match the tab, the search or the radius.
    function render() {
        const query = search.value.trim().toLowerCase();
        const maxRadius = Number(radius.value);
        radiusValue.textContent = window.resqRadius.format(maxRadius);
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
        if (currentLocation) {
            locationStatus.textContent = `${visible} elementer vist, sortert etter avstand fra din posisjon.`;
        } else {
            locationStatus.textContent = `${visible} elementer vist. Tillat posisjon for å sortere og filtrere etter avstand.`;
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
        const isOpen = !list.classList.toggle("is-collapsed");
        resultsToggle.classList.toggle("is-open", isOpen);
        resultsToggle.setAttribute("aria-expanded", String(isOpen));
    });

    search.addEventListener("input", render);
    radius.addEventListener("change", render);
    radius.addEventListener("input", render);
    exportToggle?.addEventListener("click", () => {
        if (!exportOptions) return;
        exportOptions.hidden = false;
        exportToggle.hidden = true;
        exportToggle.setAttribute("aria-expanded", "true");
    });
    exportButtons.forEach(button => {
        button.addEventListener("click", () => exportResources(button.dataset.feedExport));
    });
    exportBackButton?.addEventListener("click", () => {
        if (!exportOptions || !exportToggle) return;
        exportOptions.hidden = true;
        exportToggle.hidden = false;
        exportToggle.setAttribute("aria-expanded", "false");
        exportToggle.focus();
    });
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
            if (filterControls) {
                filterControls.classList.remove("is-open");
                filterToggle?.setAttribute("aria-expanded", "false");
            }
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
            showCurrentLocation();
            render();
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
    addFeedMarkers();
    render();
})();

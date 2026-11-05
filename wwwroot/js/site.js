window.addEventListener("resize", () => window.resqMap?.invalidateSize());

// Colours for the map markers. The names match the ColorKey values from the server.
window.resqMapColors = {
    "critical-need": "#ef4444",
    "planned-need": "#f59e0b",
    "available-resource": "#22c55e",
    "public-team": "#3b82f6"
};

document.querySelectorAll("[data-map-legend-toggle]").forEach(toggle => {
    toggle.addEventListener("click", () => {
        const legend = toggle.closest("[data-map-legend]");
        const content = legend?.querySelector(".map-color-legend-content");
        if (!content) return;

        const isExpanded = !content.classList.toggle("is-collapsed");
        toggle.setAttribute("aria-expanded", String(isExpanded));
        toggle.querySelector("i")?.classList.toggle("bi-chevron-down", isExpanded);
        toggle.querySelector("i")?.classList.toggle("bi-chevron-up", !isExpanded);
    });
});

window.resqRegistration = {
    // Combines a date field and a time field into one hidden value that is sent to the server.
    initializeDateTimeFields(formId, mappings) {
        const form = document.getElementById(formId);
        if (!form) return;

        const findInput = id => form.querySelector(`#${id}`) || document.getElementById(id);
        const updateValue = mapping => {
            const dateInput = findInput(mapping.dateId);
            const timeInput = findInput(mapping.timeId);
            const valueInput = findInput(mapping.valueId);
            if (!dateInput || !timeInput || !valueInput) return;

            valueInput.value = dateInput.value
                ? `${dateInput.value}T${timeInput.value || "00:00"}`
                : "";
        };

        const syncValues = () => mappings.forEach(updateValue);
        mappings.forEach(mapping => {
            [mapping.dateId, mapping.timeId].forEach(inputId =>
                findInput(inputId)?.addEventListener("input", syncValues));
        });
        form.addEventListener("submit", event => {
            syncValues();
            const start = findInput(mappings[0].valueId);
            const end = findInput(mappings[1].valueId);
            const endDate = findInput(mappings[1].dateId);
            const rangeError = findInput(mappings[1].rangeErrorId);
            const validRange = window.resqDateRange.validate(start, end, endDate, rangeError);

            if (!validRange) {
                event.preventDefault();
            }
        });
        syncValues();
    },

    bindCurrentLocationButton(buttonId, { onPosition }) {
        const button = document.getElementById(buttonId);
        if (!button) return;

        button.addEventListener("click", () => {
            const errorElement = document.getElementById("posError");
            if (!navigator.geolocation) {
                if (errorElement) {
                    errorElement.textContent = "Nåværende posisjon er ikke tilgjengelig i denne nettleseren.";
                }
                return;
            }

            button.disabled = true;
            if (errorElement) {
                errorElement.textContent = "";
            }
            navigator.geolocation.getCurrentPosition(position => {
                onPosition(position.coords.latitude, position.coords.longitude);
                button.disabled = false;
            }, () => {
                if (errorElement) {
                    errorElement.textContent = "Kunne ikke hente posisjonen din.";
                }
                button.disabled = false;
            });
        });
    }
};

// Checks that the end date is not before the start date.
window.resqDateRange = {
    message: "Sluttdato kan ikke være før startdato.",

    validate(startInput, endInput, endDateInput, errorElement) {
        const invalidRange = Boolean(startInput?.value && endInput?.value &&
            endInput.value < startInput.value);
        const message = invalidRange ? window.resqDateRange.message : "";

        endDateInput?.setCustomValidity(message);
        if (errorElement) {
            errorElement.textContent = message;
        }

        return !invalidRange;
    },

    bindFilter(formId, startId, endId) {
        const form = document.getElementById(formId);
        const startInput = document.getElementById(startId);
        const endInput = document.getElementById(endId);
        if (!form || !startInput || !endInput) return;

        const validate = () => window.resqDateRange.validate(
            startInput,
            endInput,
            endInput,
            null);

        [startInput, endInput].forEach(input => input.addEventListener("input", validate));
        form.addEventListener("submit", event => {
            if (!validate()) {
                event.preventDefault();
            }
        });
    }
};

window.resqRadius = {
    format(value) {
        return Number(value) === 0 ? "Alle avstander" : `${value} km`;
    },

    initialize(sliderId, valueId) {
        const slider = document.getElementById(sliderId);
        const output = document.getElementById(valueId);
        if (!slider || !output) return;

        const update = () => {
            output.textContent = window.resqRadius.format(slider.value);
        };

        slider.addEventListener("input", update);
        update();
    },

    // Hides the cards that are farther away than the slider value.
    // The slider value is remembered in localStorage.
    initializeDistanceFilter({ sliderId, valueId, errorId, cardSelector, storageKey }) {
        const slider = document.getElementById(sliderId);
        const output = document.getElementById(valueId);
        const error = document.getElementById(errorId);
        if (!slider || !output || !error) return;

        let userLocation = null;
        let locationRequestPending = false;
        let locationUnavailable = false;

        const distanceInKilometres = (latitude, longitude) => {
            const earthRadius = 6371;
            const latitudeDifference = (latitude - userLocation.latitude) * Math.PI / 180;
            const longitudeDifference = (longitude - userLocation.longitude) * Math.PI / 180;
            const firstLatitude = userLocation.latitude * Math.PI / 180;
            const secondLatitude = latitude * Math.PI / 180;
            const haversine = Math.sin(latitudeDifference / 2) ** 2
                + Math.cos(firstLatitude) * Math.cos(secondLatitude)
                * Math.sin(longitudeDifference / 2) ** 2;

            return earthRadius * 2 * Math.atan2(
                Math.sqrt(haversine),
                Math.sqrt(1 - haversine));
        };

        const applyFilter = () => {
            const radius = Number(slider.value);
            document.querySelectorAll(cardSelector).forEach(card => {
                const latitude = Number(card.dataset.latitude);
                const longitude = Number(card.dataset.longitude);
                const hasCoordinates = Number.isFinite(latitude) && Number.isFinite(longitude);

                card.hidden = radius > 0 && userLocation && hasCoordinates
                    ? distanceInKilometres(latitude, longitude) > radius
                    : false;
            });
        };

        const requestLocation = () => {
            if (userLocation || locationRequestPending || locationUnavailable) {
                applyFilter();
                return;
            }
            if (!navigator.geolocation) {
                locationUnavailable = true;
                error.hidden = false;
                return;
            }

            locationRequestPending = true;
            navigator.geolocation.getCurrentPosition(position => {
                locationRequestPending = false;
                userLocation = {
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude
                };
                error.hidden = true;
                applyFilter();
            }, () => {
                locationRequestPending = false;
                locationUnavailable = true;
                error.hidden = false;
                slider.value = "0";
                output.textContent = window.resqRadius.format("0");
                if (storageKey) {
                    localStorage.setItem(storageKey, "0");
                }
                applyFilter();
            });
        };

        slider.addEventListener("input", () => {
            output.textContent = window.resqRadius.format(slider.value);
            if (storageKey) {
                localStorage.setItem(storageKey, slider.value);
            }
            if (Number(slider.value) > 0) {
                requestLocation();
            } else {
                error.hidden = true;
                applyFilter();
            }
        });

        const savedRadius = storageKey && localStorage.getItem(storageKey);
        if (savedRadius !== null && Number(savedRadius) >= 0 && Number(savedRadius) <= 100) {
            slider.value = savedRadius;
        }
        output.textContent = window.resqRadius.format(slider.value);
        if (Number(slider.value) > 0) {
            requestLocation();
        }
    }
};

// Dark and light mode button. The choice is saved in localStorage.
const themeToggle = document.querySelector("#theme-toggle");

if (themeToggle) {
    const themeToggleLabel = themeToggle.querySelector("#theme-toggle-label");
    const themeToggleIcon = themeToggle.querySelector("i");
    const root = document.documentElement;

    const updateThemeToggle = () => {
        const isDark = root.dataset.theme === "dark";

        themeToggle.setAttribute("aria-pressed", String(isDark));
        themeToggleLabel.textContent = isDark ? "Lys modus" : "Mørk modus";
        themeToggleIcon.className = isDark ? "bi bi-sun" : "bi bi-moon";
    };

    themeToggle.addEventListener("click", () => {
        const theme = root.dataset.theme === "dark" ? "light" : "dark";

        root.dataset.theme = theme;
        root.dataset.bsTheme = theme;
        localStorage.setItem("resq-theme", theme);
        updateThemeToggle();
    });

    updateThemeToggle();
}

// onsubmit and onchange written in the HTML are blocked by the Content Security Policy, so they are handled here

// A form with data-confirm asks the user before it is sent
document.addEventListener("submit", event => {
    const message = event.target.dataset?.confirm;
    if (message && !window.confirm(message)) {
        event.preventDefault();
    }
});

// A select with data-auto-submit sends its form when the value changes
document.addEventListener("change", event => {
    if (event.target.matches?.("[data-auto-submit]")) {
        event.target.form?.submit();
    }
});

// A dialog that shows the demo 2FA code, with a button that copies it.
const demoCodeDialog = document.querySelector("#demo-code-dialog");

if (demoCodeDialog) {
    const copyButton = demoCodeDialog.querySelector("[data-copy-code]");
    const copyStatus = demoCodeDialog.querySelector(".auth-code-copy-status");

    copyButton?.addEventListener("click", async () => {
        const code = copyButton.dataset.copyCode ?? "";

        try {
            await navigator.clipboard.writeText(code);
            demoCodeDialog.remove();
        } catch {
            if (copyStatus) copyStatus.textContent = "Kunne ikke kopiere koden.";
        }
    });
}

// The boxes for the 2FA code: move between the boxes, accept paste, and join the digits into one value.
const codeForm = document.querySelector("[data-code-form]");

if (codeForm) {
    const codeInputs = [...codeForm.querySelectorAll("[data-code-inputs] .auth-code-input")];
    const codeValue = codeForm.querySelector("[data-code-value]");

    const syncCodeValue = () => {
        if (codeValue) codeValue.value = codeInputs.map(input => input.value).join("");
    };

    const setCode = value => {
        [...value].slice(0, codeInputs.length).forEach((digit, index) => {
            codeInputs[index].value = /\d/.test(digit) ? digit : "";
        });
        syncCodeValue();
        codeInputs[Math.min(value.length, codeInputs.length) - 1]?.focus();
    };

    codeInputs.forEach((input, index) => {
        input.addEventListener("input", () => {
            input.value = input.value.replace(/\D/g, "").slice(-1);
            syncCodeValue();
            if (input.value && index < codeInputs.length - 1) {
                codeInputs[index + 1].focus();
            }
        });

        input.addEventListener("keydown", event => {
            if (event.key === "Backspace" && !input.value && index > 0) {
                codeInputs[index - 1].focus();
            }
        });

        input.addEventListener("paste", event => {
            event.preventDefault();
            setCode((event.clipboardData?.getData("text") ?? "").replace(/\D/g, ""));
        });
    });

    codeForm.addEventListener("submit", syncCodeValue);
}

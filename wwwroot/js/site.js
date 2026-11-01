window.addEventListener("resize", () => window.resqMap?.invalidateSize());

window.resqRegistration = {
    initializeDateTimeFields(formId, mappings) {
        const form = document.getElementById(formId);
        if (!form) return;

        const updateValue = mapping => {
            const dateInput = document.getElementById(mapping.dateId);
            const timeInput = document.getElementById(mapping.timeId);
            const valueInput = document.getElementById(mapping.valueId);
            if (!dateInput || !timeInput || !valueInput) return;

            valueInput.value = dateInput.value
                ? `${dateInput.value}T${timeInput.value || "00:00"}`
                : "";
        };

        const syncValues = () => mappings.forEach(updateValue);
        mappings.forEach(mapping => {
            [mapping.dateId, mapping.timeId].forEach(inputId =>
                document.getElementById(inputId)?.addEventListener("input", syncValues));
        });
        form.addEventListener("submit", syncValues);
        syncValues();
    },

    bindCurrentLocationButton(buttonId, { onPosition }) {
        const button = document.getElementById(buttonId);
        if (!button) return;

        button.addEventListener("click", () => {
            const errorElement = document.getElementById("posError");
            if (!navigator.geolocation) {
                if (errorElement) {
                    errorElement.textContent = "Current location is not available in this browser.";
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
                    errorElement.textContent = "Could not get the current location.";
                }
                button.disabled = false;
            });
        });
    }
};

window.resqRadius = {
    format(value) {
        return Number(value) === 0 ? "All distances" : `${value} km`;
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
    }
};

const themeToggle = document.querySelector("#theme-toggle");

if (themeToggle) {
    const themeToggleLabel = themeToggle.querySelector("#theme-toggle-label");
    const themeToggleIcon = themeToggle.querySelector("i");
    const root = document.documentElement;

    const updateThemeToggle = () => {
        const isDark = root.dataset.theme === "dark";

        themeToggle.setAttribute("aria-pressed", String(isDark));
        themeToggleLabel.textContent = isDark ? "Light mode" : "Dark mode";
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

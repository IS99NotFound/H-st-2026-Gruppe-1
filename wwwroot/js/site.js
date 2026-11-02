window.addEventListener("resize", () => window.resqMap?.invalidateSize());

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
            const invalidRange = start?.value && end?.value && end.value < start.value;

            if (endDate) {
                endDate.setCustomValidity(invalidRange
                    ? "End date cannot be before start date."
                    : "");
            }
            if (rangeError) {
                rangeError.textContent = invalidRange
                    ? "End date cannot be before start date."
                    : "";
            }
            if (invalidRange) {
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
            if (copyStatus) copyStatus.textContent = "Could not copy the code.";
        }
    });
}

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

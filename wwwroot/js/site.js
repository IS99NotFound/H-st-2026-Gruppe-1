window.addEventListener("resize", () => window.resqMap?.invalidateSize());

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

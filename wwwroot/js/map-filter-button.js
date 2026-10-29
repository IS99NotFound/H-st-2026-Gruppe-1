document.addEventListener("DOMContentLoaded", function () {
    const filterPanel = document.getElementById("survival-filter-panel");
    const filterButton = document.getElementById("survival-filter-button");
    const filterContent = document.getElementById("survival-filter-content");

    //Avsluttes trygt on siden ikke har et filter panel.
    if (!filterPanel || !filterButton || !filterContent) {
        return;
    }

    const setFilterState = function (isCollapsed) {
        filterPanel.classList.toggle("is-collapsed", isCollapsed);
        filterButton.setAttribute("aria-expanded", String(!isCollapsed));
        filterButton.setAttribute(
            "aria-label",
            isCollapsed ? "Expand map filters" : "Collapse map filters"
        );
        filterButton.innerHTML = isCollapsed
            ? '<i class="bi bi-chevron-right" aria-hidden="true"></i>'
            : '<i class="bi bi-chevron-left" aria-hidden="true"></i>';
    };

    setFilterState(true);
    filterButton.addEventListener("click", function () {
        const isCollapsed = filterPanel.classList.contains("is-collapsed");
        setFilterState(!isCollapsed);
    });
});

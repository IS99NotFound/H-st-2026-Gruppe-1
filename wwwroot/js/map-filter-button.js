document.addEventListener("DOMContentLoaded", function () {
    const filterPanel = document.getElementById("survival-filter-panel");
    const filterButton = document.getElementById("survival-filter-button");
    const filterContent = document.getElementById("survival-filter-content");

    //Avsluttes trygt on siden ikke har et filter panel.
    if (!filterPanel || !filterButton || !filterContent) {
        return;
    }

    filterButton.addEventListener("click", function () {
        const isCollapsed = filterPanel.classList.contains("is-collapsed");

        filterButton.setAttribute("aria-expanded", String(!isCollapsed));

        if (isCollapsed) {

            filterPanel.classList.remove("is-collapsed");
            //Viser pilen som lar bruker åpne filter.
            filterButton.innerHTML =
            '<i class="bi bi-chevron-left" aria-hidden="true"></i>';

            filterButton.setAttribute("aria-expanded", "true");
            filterButton.setAttribute(
                "aria-label",
                "Collapse survival locations-filters"
            );

        } else {
            filterPanel.classList.add("is-collapsed");
            
            //Viser pilen som lar bruker lukke filter.
            filterButton.innerHTML = '<i class ="bi bi-chevron-right" aria-hidden="true"></i>';

            filterButton.setAttribute("aria-expanded", "false");
            filterButton.setAttribute(
                "aria-label",
                "Expand survival locations-filters"
            );
        }
    });
});

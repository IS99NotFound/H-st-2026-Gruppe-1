// Collect phonepanel status so buttons works the same on all pages
function initSiteBindings() {
	if (window.__resq_site_initialized) return;
	window.__resq_site_initialized = true;

	const mainLayout = document.querySelector('.main-layout');
	const sidePanel = document.querySelector('#side-panel');
	const panelButtons = Array.from(document.querySelectorAll('[data-panel-toggle], .panel-close'));

	function setPanelOpen(isOpen) {
		if (!mainLayout || !sidePanel) return;

		mainLayout.classList.toggle('panel-open', isOpen);
		panelButtons.forEach((button) => {
			button.setAttribute('aria-expanded', String(isOpen));
		});

		// Leaflet measures map size anew when panel size changes Leaflet 
		window.requestAnimationFrame(() => window.resqMap?.invalidateSize());
	}

	panelButtons.forEach((button) => {
		button.addEventListener('click', () => {
			if (!mainLayout || !sidePanel) {
				const homeUrl = button.dataset.homeUrl;
				// On sub-pages, move to map and open resource registration 
				if (homeUrl) window.location.href = `${homeUrl}?openResource=true`;
				return;
			}

			setPanelOpen(!mainLayout.classList.contains('panel-open'));
		});
	});

	if (mainLayout && sidePanel && new URLSearchParams(window.location.search).get('openResource') === 'true') {
		window.requestAnimationFrame(() => setPanelOpen(true));
	}

	document.addEventListener('keydown', (event) => {
		if (event.key === 'Escape' && mainLayout?.classList.contains('panel-open')) {
			setPanelOpen(false);
			document.querySelector('.mobile-panel-button')?.focus();
		}
	});

	window.addEventListener('resize', () => window.resqMap?.invalidateSize());
}

if (document.readyState === 'loading') {
	document.addEventListener('DOMContentLoaded', initSiteBindings);
} else {
	initSiteBindings();
}

(function () {
	function initHighlight() {
		if (typeof hljs === 'undefined') {
			setTimeout(initHighlight, 500);
			return;
		}

		hljs.highlightAll();

		const observer = new MutationObserver(() => {
			const unhighlighted = document.querySelectorAll('pre code:not(.hljs)');
			unhighlighted.forEach((el) => {
				try {
					hljs.highlightElement(el);
				} catch (e) {

				}
			});
		});

		observer.observe(document.body, { childList: true, subtree: true });
	}

	if (document.readyState === 'loading') {
		document.addEventListener('DOMContentLoaded', initHighlight);
	} else {
		initHighlight();
	}
})();

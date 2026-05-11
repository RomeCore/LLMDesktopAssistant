window.isScrolledToBottom = function (elementId) {
	const element = document.getElementById(elementId);
	if (!element) return true;
	// Scroll threshold is set to 200 pixels from the bottom of the element
	return element.scrollHeight - element.scrollTop - element.clientHeight < 200;
};

window.scrollToBottom = function (elementId) {
	const element = document.getElementById(elementId);
	if (element) {
		element.scrollTop = element.scrollHeight;
	}
};

window.scrollToBottomIfNeeded = function (elementId) {
	const element = document.getElementById(elementId);
	if (!element) return;
	if (element.scrollHeight - element.scrollTop - element.clientHeight < 200) {
		element.scrollTop = element.scrollHeight;
	}
};

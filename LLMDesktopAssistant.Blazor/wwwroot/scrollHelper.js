window.scrollToBottom = function (elementId) {
	const element = document.getElementById(elementId);
	if (element) {
		element.scrollTop = element.scrollHeight;
	}
};

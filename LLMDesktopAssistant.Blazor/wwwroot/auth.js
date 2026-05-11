// Auth helper functions for WebUI
// Handles authentication via cookie-based API

window.checkAuth = async function () {
	try {
		const response = await fetch('/api/auth/me');
		return response.ok;
	} catch {
		return false;
	}
};

window.loginUser = async function (login, password) {
	try {
		const response = await fetch('/api/auth/login', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ login, password })
		});
		return response.ok;
	} catch {
		return false;
	}
};

window.logoutUser = async function () {
	try {
		await fetch('/api/auth/logout', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' }
		});
	} catch {
		// ignore
	}
	window.location.replace('/login');
};

window.locationReplace = function (url) {
	window.location.replace(url);
};

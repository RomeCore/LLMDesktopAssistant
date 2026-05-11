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

window.selectProfileImage = function () {
	return new Promise((resolve, reject) => {
		const input = document.createElement('input');
		input.type = 'file';
		input.accept = 'image/png,image/jpeg,image/gif,image/bmp';
		input.onchange = async function (e) {
			const file = e.target.files?.[0];
			if (!file) {
				resolve(null);
				return;
			}
			
			try {
				// Resize to 128x128 using canvas
				const img = await createImageBitmap(file);
				const canvas = document.createElement('canvas');
				canvas.width = 128;
				canvas.height = 128;
				const ctx = canvas.getContext('2d');
				
				// Crop center
				const size = Math.min(img.width, img.height);
				const sx = (img.width - size) / 2;
				const sy = (img.height - size) / 2;
				ctx.drawImage(img, sx, sy, size, size, 0, 0, 128, 128);
				
				const base64 = canvas.toDataURL('image/png').split(',')[1];
				resolve(base64);
			} catch (err) {
				reject(err);
			}
		};
		input.click();
	});
};

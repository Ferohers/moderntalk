/**
 * ModernUO Web Portal - API Client
 * Handles all communication with the server API.
 * Tokens are stored in HttpOnly cookies set by the server.
 */
const Api = {
    /**
     * Make an authenticated API request
     */
    async request(url, options = {}) {
        const config = {
            headers: {
                'Content-Type': 'application/json',
                ...options.headers
            },
            credentials: 'same-origin', // Include cookies
            ...options
        };

        try {
            const response = await fetch(url, config);

            // Handle 401 - try to refresh token
            if (response.status === 401) {
                const refreshed = await this.refreshToken();
                if (refreshed) {
                    // Retry the original request
                    return this.request(url, options);
                }
                // Refresh failed - user needs to login again
                return { error: 'Session expired. Please login again.' };
            }

            // Handle rate limiting
            if (response.status === 429) {
                const retryAfter = response.headers.get('Retry-After');
                return { error: `Too many requests. Please wait ${retryAfter || '60'} seconds.` };
            }

            // Parse response
            const data = await response.json().catch(() => ({}));

            if (!response.ok) {
                return { error: data.error || `Request failed (${response.status})` };
            }

            return data;
        } catch (err) {
            return { error: 'Network error. Please check your connection.' };
        }
    },

    /**
     * Get server information (public endpoint)
     */
    async getServerInfo() {
        return this.request('/api/server/info', { method: 'GET' });
    },

    /**
     * Register a new account
     */
    async register(username, password, confirmPassword, email) {
        const body = {
            username: username,
            password: password,
            confirmPassword: confirmPassword
        };

        if (email) {
            body.email = email;
        }

        return this.request('/api/auth/register', {
            method: 'POST',
            body: JSON.stringify(body)
        });
    },

    /**
     * Login with credentials
     */
    async login(username, password) {
        return this.request('/api/auth/login', {
            method: 'POST',
            body: JSON.stringify({
                username: username,
                password: password
            })
        });
    },

    /**
     * Refresh the access token using the refresh token cookie
     */
    async refreshToken() {
        try {
            // Refresh token is sent automatically via HttpOnly cookie
            const response = await fetch('/api/auth/refresh', {
                method: 'POST',
                credentials: 'same-origin'
            });

            return response.ok;
        } catch {
            return false;
        }
    },

    /**
     * Logout - invalidate tokens
     */
    async logout() {
        return this.request('/api/auth/logout', {
            method: 'POST',
            body: JSON.stringify({})
        });
    },

    /**
     * Get account information (requires auth)
     */
    async getAccountInfo() {
        return this.request('/api/account/info', { method: 'GET' });
    },

    /**
     * Change password (requires auth)
     */
    async changePassword(currentPassword, newPassword, confirmNewPassword) {
        return this.request('/api/account/change-password', {
            method: 'POST',
            body: JSON.stringify({
                currentPassword: currentPassword,
                newPassword: newPassword,
                confirmNewPassword: confirmNewPassword
            })
        });
    },

    /**
     * Request a password reset email
     */
    async forgotPassword(username, email) {
        return this.request('/api/auth/forgot-password', {
            method: 'POST',
            body: JSON.stringify({
                username: username,
                email: email
            })
        });
    },

    /**
     * Reset password using a reset token
     */
    async resetPassword(resetToken, newPassword, confirmNewPassword) {
        return this.request('/api/auth/reset-password', {
            method: 'POST',
            body: JSON.stringify({
                resetToken: resetToken,
                newPassword: newPassword,
                confirmNewPassword: confirmNewPassword
            })
        });
    },

    /**
     * Check if the user is currently authenticated
     * Makes a lightweight request to the account info endpoint
     */
    async isAuthenticated() {
        try {
            const response = await fetch('/api/account/info', {
                method: 'GET',
                credentials: 'same-origin'
            });
            return response.ok;
        } catch {
            return false;
        }
    }
};

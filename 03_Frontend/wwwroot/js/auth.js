/**
 * VisorDatosSIG 2026 — Módulo de Autenticación y Control de Sesión / Roles
 */

const AuthService = {
    SESSION_KEY: 'visordatos_sig_session',

    getSession() {
        try {
            const data = sessionStorage.getItem(this.SESSION_KEY);
            return data ? JSON.parse(data) : null;
        } catch {
            return null;
        }
    },

    setSession(user, token) {
        sessionStorage.setItem(this.SESSION_KEY, JSON.stringify({ user, token, timestamp: Date.now() }));
    },

    clearSession() {
        sessionStorage.removeItem(this.SESSION_KEY);
    },

    isAuthenticated() {
        return this.getSession() !== null;
    },

    isAdmin() {
        const session = this.getSession();
        return session && session.user && (session.user.idRol === 1 || session.user.rol === 'Administrador');
    },

    isConsultor() {
        const session = this.getSession();
        return session && session.user && (session.user.idRol === 2 || session.user.rol === 'Consultor');
    },

    async login(username, password) {
        try {
            const res = await fetch('/api/autenticacion/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password })
            });

            const data = await res.json();
            if (res.ok && data.exitoso) {
                this.setSession(data.usuario, data.token);
                return { success: true, data };
            } else {
                return { success: false, message: data.mensaje || 'Credenciales incorrectas o cuenta bloqueada.' };
            }
        } catch (err) {
            return { success: false, message: 'Error de comunicación con el servidor de autenticación.' };
        }
    },

    async logout() {
        try {
            await fetch('/api/autenticacion/logout', { method: 'POST' });
        } catch (e) {
            console.warn('Error al notificar logout al servidor', e);
        }
        this.clearSession();
        window.location.href = '/Login';
    },

    async syncWithServer() {
        try {
            const res = await fetch('/api/autenticacion/usuario-actual');
            if (res.ok) {
                const info = await res.json();
                if (info.autenticado) {
                    this.setSession({
                        idUsuario: parseInt(info.idUsuario) || 0,
                        username: info.username,
                        nombreCompleto: info.nombreCompleto,
                        email: info.email,
                        rol: info.rol,
                        idRol: parseInt(info.idRol) || 2
                    }, null);
                } else {
                    this.clearSession();
                    const isLoginPage = window.location.pathname.toLowerCase().includes('/login');
                    if (!isLoginPage) {
                        window.location.href = '/Login';
                    }
                }
            }
        } catch (e) {
            console.warn('No se pudo sincronizar sesión con el servidor:', e);
        }
    }
};

// ==========================================================================
// Módulo de Control de Tema (Modo Claro / Modo Oscuro)
// ==========================================================================
const ThemeService = {
    THEME_KEY: 'sig_theme',

    getTheme() {
        try {
            return localStorage.getItem(this.THEME_KEY) || 'dark';
        } catch {
            return 'dark';
        }
    },

    setTheme(theme) {
        try {
            localStorage.setItem(this.THEME_KEY, theme);
        } catch { }

        document.documentElement.setAttribute('data-bs-theme', theme);
        document.documentElement.setAttribute('data-theme', theme);
        document.body.classList.remove('theme-dark', 'theme-light');
        document.body.classList.add(theme === 'light' ? 'theme-light' : 'theme-dark');
        
        this.updateToggleIcon(theme);
        window.dispatchEvent(new CustomEvent('sig-theme-changed', { detail: { theme } }));
    },

    toggleTheme() {
        const nextTheme = this.getTheme() === 'dark' ? 'light' : 'dark';
        this.setTheme(nextTheme);
        return nextTheme;
    },

    updateToggleIcon(theme) {
        const icon = document.getElementById('themeIcon');
        const btn = document.getElementById('btnThemeToggle');
        const label = document.getElementById('themeLabel');
        const dropdownBadge = document.getElementById('dropdownThemeBadge');
        const cardDark = document.getElementById('btnThemeCardDark');
        const cardLight = document.getElementById('btnThemeCardLight');

        if (icon) {
            if (theme === 'light') {
                icon.className = 'bi bi-sun-fill text-warning';
                if (label) label.textContent = 'Modo Claro';
                if (btn) btn.title = 'Cambiar a modo oscuro';
            } else {
                icon.className = 'bi bi-moon-stars-fill text-warning';
                if (label) label.textContent = 'Modo Oscuro';
                if (btn) btn.title = 'Cambiar a modo claro';
            }
        }

        if (dropdownBadge) {
            dropdownBadge.textContent = theme === 'light' ? 'Claro ☀️' : 'Oscuro 🌙';
            dropdownBadge.className = theme === 'light' ? 'badge bg-warning-subtle text-warning border small' : 'badge bg-info-subtle text-info border small';
        }

        if (cardDark && cardLight) {
            if (theme === 'dark') {
                cardDark.classList.add('active');
                cardLight.classList.remove('active');
            } else {
                cardLight.classList.add('active');
                cardDark.classList.remove('active');
            }
        }
    },

    init() {
        const current = this.getTheme();
        this.setTheme(current);
        const btn = document.getElementById('btnThemeToggle');
        if (btn) {
            btn.addEventListener('click', (e) => {
                e.preventDefault();
                this.toggleTheme();
            });
        }
    }
};

// Utilidad Global de Toasts
function showToast(title, message, type = 'info') {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const id = 'toast_' + Date.now();
    const bgClass = type === 'success' ? 'text-bg-success' : (type === 'danger' || type === 'error' ? 'text-bg-danger' : (type === 'warning' ? 'text-bg-warning' : 'text-bg-primary'));

    const html = `
        <div id="${id}" class="toast align-items-center ${bgClass} border-0 shadow-lg" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    <strong>${title}:</strong> ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', html);
    const toastElem = document.getElementById(id);
    if (toastElem && typeof bootstrap !== 'undefined') {
        const bsToast = new bootstrap.Toast(toastElem, { delay: 4500 });
        bsToast.show();
        toastElem.addEventListener('hidden.bs.toast', () => toastElem.remove());
    }
}

// Inicializar al cargar DOM
document.addEventListener('DOMContentLoaded', () => {
    // Inicializar Tema Claro / Oscuro
    ThemeService.init();

    // Sincronizar estado de sesión
    AuthService.syncWithServer();
});

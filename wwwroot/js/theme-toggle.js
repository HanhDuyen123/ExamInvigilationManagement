(function () {
    const storageKey = 'exam-ui-theme';
    const root = document.documentElement;

    function systemTheme() {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function currentTheme() {
        return localStorage.getItem(storageKey) || systemTheme();
    }

    function applyTheme(theme) {
        const normalized = theme === 'dark' ? 'dark' : 'light';
        root.setAttribute('data-theme', normalized);
        if (document.body) document.body.setAttribute('data-theme', normalized);

        document.querySelectorAll('.theme-toggle-text').forEach(function (item) {
            item.textContent = normalized === 'dark' ? 'Light' : 'Dark';
        });

        document.querySelectorAll('.theme-toggle-btn').forEach(function (button) {
            button.setAttribute('aria-pressed', normalized === 'dark' ? 'true' : 'false');
            button.setAttribute('title', normalized === 'dark' ? 'Chuyển sang giao diện sáng' : 'Chuyển sang giao diện tối');
        });
    }

    applyTheme(currentTheme());

    document.addEventListener('click', function (event) {
        const button = event.target.closest('.theme-toggle-btn');
        if (!button) return;

        const next = (root.getAttribute('data-theme') || currentTheme()) === 'dark' ? 'light' : 'dark';
        localStorage.setItem(storageKey, next);
        applyTheme(next);
    });
})();

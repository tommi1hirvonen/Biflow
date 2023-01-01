const storedTheme = localStorage.getItem('theme')

function getPreferredTheme() {
    if (storedTheme) {
        return storedTheme;
    }
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

function setTheme(theme) {
    if (theme === 'auto' && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        document.documentElement.setAttribute('data-bs-theme', 'dark');
    } else {
        document.documentElement.setAttribute('data-bs-theme', theme);
    }
}

(() => {
    setTheme(getPreferredTheme());
})();

function showActiveTheme(theme) {
    const btnToActive = document.querySelector(`[data-bs-theme-value="${theme}"]`);

    document.querySelectorAll('[data-bs-theme-value]').forEach(element => {
        element.classList.remove('active');
    })

    btnToActive.classList.add('active');
}

window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    if (storedTheme !== 'light' || storedTheme !== 'dark') {
        setTheme(getPreferredTheme());
    }
})

function addThemeToggleListeners() {
    showActiveTheme(getPreferredTheme());

    document.querySelectorAll('[data-bs-theme-value]')
        .forEach(toggle => {
            toggle.addEventListener('click', () => {
                const theme = toggle.getAttribute('data-bs-theme-value');
                localStorage.setItem('theme', theme);
                setTheme(theme);
                showActiveTheme(theme);
            })
        });
}
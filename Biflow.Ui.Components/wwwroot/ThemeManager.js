export function getPreferredTheme() {
    const theme = localStorage.getItem('theme');
    if (theme) {
        return theme;
    }
    return 'auto';
}

export function setTheme(theme) {
    let effectiveTheme;
    if (theme === 'auto' && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        effectiveTheme = 'dark';
    } else {
        document.documentElement.setAttribute('data-bs-theme', theme);
        effectiveTheme = theme === 'dark' ? theme : 'light';
    }
    localStorage.setItem('theme', theme);
    return effectiveTheme;
}

export function setPreferredThemeChangedListener(dotnetObjectReference) {
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', async () => {
        const theme = getPreferredTheme();
        if (theme === 'auto') {
            var effectiveTheme = setTheme(theme);
            await dotnetObjectReference.invokeMethodAsync("UpdateTheme", effectiveTheme);
        }
    })
}
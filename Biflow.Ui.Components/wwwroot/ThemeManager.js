const storedTheme = localStorage.getItem('theme')

export function getPreferredTheme() {
    if (storedTheme) {
        return storedTheme;
    }
    return 'auto';
}

export function setTheme(theme) {
    var effectiveTheme;
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
        // Mode is auto
        if (storedTheme !== 'light' || storedTheme !== 'dark') {
            var theme = getPreferredTheme();
            var effectiveTheme = setTheme(theme);
            await dotnetObjectReference.InvokeMethodAsync("UpdateTheme", effectiveTheme);
        }
    })
}
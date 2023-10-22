export function showContextMenu(dropdownElement, clientX, clientY, dotNetObjectReference) {
    dropdownElement.classList.add('show');
    clientY += document.documentElement.scrollTop;
    clientX += document.documentElement.scrollLeft;
    // Place the dropdown to the location of the mouse.
    var bodyRect = document.body.getBoundingClientRect();
    // Check whether the dropdown menu would overflow over the right side of the window.
    var tempX = clientX + dropdownElement.clientWidth <= window.innerWidth ? clientX : clientX - dropdownElement.clientWidth;
    // Check whether the dropdown menu would overflow over the bottom of the window.
    var tempY = clientY + bodyRect.top + dropdownElement.clientHeight <= window.innerHeight ? clientY : clientY - dropdownElement.clientHeight;
    dropdownElement.style.top = `${tempY}px`;
    dropdownElement.style.left = `${tempX}px`;
    window.addEventListener('scroll', async function (_) {
        dropdownElement.classList.remove('show');
        await dotNetObjectReference.invokeMethodAsync("OnMenuHidden");
    }, { once: true });
    window.addEventListener('click', async function (_) {
        dropdownElement.classList.remove('show');
        await dotNetObjectReference.invokeMethodAsync("OnMenuHidden");
    }, { once: true });
}
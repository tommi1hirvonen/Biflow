export function showContextMenu(dropdownElement, clientX, clientY, dotNetObjectReference) {
    dropdownElement.classList.add('show');
    clientY += document.documentElement.scrollTop;
    clientX += document.documentElement.scrollLeft;
    // Place the dropdown to the location of the mouse.
    const bodyRect = document.body.getBoundingClientRect();
    // Check whether the dropdown menu would overflow over the right side of the window.
    const tempX = clientX + dropdownElement.clientWidth <= window.innerWidth ? clientX : clientX - dropdownElement.clientWidth;
    // Check whether the dropdown menu would overflow over the bottom of the window.
    const tempY = clientY + bodyRect.top + dropdownElement.clientHeight <= window.innerHeight ? clientY : clientY - dropdownElement.clientHeight;
    dropdownElement.style.top = `${tempY}px`;
    dropdownElement.style.left = `${tempX}px`;
}

var listener;

export function attachWindowListeners(dropdownElement, dotNetObjectReference) {
    listener = async function (event) {
        if (dropdownElement.classList.contains("show")) {
            dropdownElement.classList.remove('show');
            await dotNetObjectReference.invokeMethodAsync("OnMenuHidden");
        }
    }
    window.addEventListener('scroll', listener);
    window.addEventListener('click', listener);
}

export function detachWindowListeners() {
    window.removeEventListener('scroll', listener);
    window.removeEventListener('click', listener);
    listener = null;
}
const blurListener = function onBlurListener(event) {
    const wrapperElement = event.target.closest('.taginput-wrapper');
    if (wrapperElement && wrapperElement.contains(event.relatedTarget) ) {
        return;
    }
    event.target.dotNetObject.invokeMethodAsync('TagInput_Hide');
};

export function calculateShowUp(dropdownElement) {
    dropdownElement.classList.add('show');
    const _vh = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;
    const space = _vh - dropdownElement.getBoundingClientRect().bottom;
    return space < 0;
}

export function create(inputElement, dotNetObject) {
    inputElement.dotNetObject = dotNetObject;
    inputElement.addEventListener('blur', blurListener);
}

export function dispose(inputElement) {
    inputElement.dotNetObject = null;
    inputElement.removeEventListener('blur', blurListener);
}
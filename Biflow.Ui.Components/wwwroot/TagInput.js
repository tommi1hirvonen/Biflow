
const listener = function onClickListener(event) {
    var elem = event.target;
    var autosuggests = document.querySelectorAll('.taginput-wrapper');
    if (!elem.closest('.taginput-wrapper')) {
        [].forEach.call(autosuggests, function (autosuggest) {
            autosuggest.dotNetObject.invokeMethodAsync('TagInput_Hide');
        });
    } else {
        [].forEach.call(autosuggests, function (autosuggest) {
            if (elem.closest('.taginput-wrapper') != autosuggest) {
                autosuggest.dotNetObject.invokeMethodAsync('TagInput_Hide');
            }
        });
    }
};

export function calculateShowUp(listWrapper) {
    listWrapper.classList.add('show');
    let _vh = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;
    let space = _vh - listWrapper.getBoundingClientRect().bottom;
    if (space < 0) {
        return true;
    } else {
        return false;
    }
}

export function create(element, dotNetObject) {
    element.dotNetObject = dotNetObject;
    document.addEventListener('click', listener);
}

export function dispose(element) {
    element.dotNetObject = null;
    document.removeEventListener('click', listener);
}
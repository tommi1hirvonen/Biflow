export function setOnContextMenuListener(containerElement, dropdownElement) {
    containerElement.addEventListener('contextmenu', function (event) {
        event.preventDefault();

        // Hide other menus
        var menus = document.querySelectorAll('.context-menu');
        for (var i = 0; i < menus.length; i++) {
            var menu = menus[i];
            menu.classList.remove('show');
        }

        var menu = dropdownElement.querySelector('.dropdown-menu');
        menu.classList.add('show');

        // Place the dropdown to the location of the mouse.
        var bodyRect = document.body.getBoundingClientRect();
        // Check whether the dropdown menu would overflow over the right side of the window.
        var tempX = event.pageX + menu.clientWidth <= bodyRect.width ? event.pageX : event.pageX - menu.clientWidth;
        // Check whether the dropdown menu would overflow over the bottom of the window.
        var tempY = event.pageY + bodyRect.top + menu.clientHeight <= bodyRect.height ? event.pageY : event.pageY - menu.clientHeight;
        dropdownElement.style.top = `${tempY}px`;
        dropdownElement.style.left = `${tempX}px`;
    }, false);
}

export function attachWindowOnClickListener() {
    window.addEventListener('click', windowOnClick);
}

export function disposeWindowOnClickListener() {
    window.removeEventListener('click', windowOnClick);
}

function windowOnClick(_) {
    var menus = document.querySelectorAll('.context-menu');
    for (var i = 0; i < menus.length; i++) {
        var menu = menus[i];
        menu.classList.remove('show');
    }
}
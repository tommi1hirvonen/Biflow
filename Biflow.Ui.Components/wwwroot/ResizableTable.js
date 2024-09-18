export function createResizableTable(table, dotNetObjectReference) {
    const cols = table.querySelectorAll('th');
    [].forEach.call(cols, function (col) {
        // Add a resizer element to the column
        const resizer = document.createElement('div');
        resizer.classList.add('resizer');
        // Set the height
        resizer.style.height = `${table.offsetHeight}px`;
        col.appendChild(resizer);
        createResizableColumn(col, resizer, dotNetObjectReference);
    });
}

function createResizableColumn(col, resizer, dotNetObjectReference) {
    let x = 0;
    let w = 0;

    const mouseDownHandler = function (e) {
        x = e.clientX;

        const styles = window.getComputedStyle(col);
        w = parseInt(styles.width, 10);

        document.addEventListener('mousemove', mouseMoveHandler);
        document.addEventListener('mouseup', mouseUpHandler);

        resizer.classList.add('resizing');
    };

    const mouseMoveHandler = function (e) {
        const dx = e.clientX - x;
        col.style.width = `${w + dx}px`;
    };

    const mouseUpHandler = async function () {
        resizer.classList.remove('resizing');
        document.removeEventListener('mousemove', mouseMoveHandler);
        document.removeEventListener('mouseup', mouseUpHandler);
        await dotNetObjectReference.invokeMethodAsync("SetColumnWidthAsync", col.id, col.style.width);
    };

    resizer.addEventListener('mousedown', mouseDownHandler);
}
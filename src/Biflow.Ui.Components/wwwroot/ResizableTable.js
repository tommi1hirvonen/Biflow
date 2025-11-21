export function createResizableTable(table, dotNetObjectReference) {
    const cols = table.querySelectorAll('th');
    const resizableColumns = Array.from(cols).map(function (col) {
        // Add a resizer element to the column
        const resizer = document.createElement('div');
        resizer.classList.add('resizer');
        // Set the height
        // TODO Set the height in a way that it reacts to the parent table's height.
        resizer.style.height = `${table.offsetHeight}px`;
        col.appendChild(resizer);
        return createResizableColumn(col, resizer, dotNetObjectReference);
    });
    return {
        dispose: () => resizableColumns.forEach(col => col.dispose())
    }
}

function createResizableColumn(col, resizer, dotNetHelper) {
    let isResizing = false;
    let startX = 0;
    let startLeftWidth = 0;

    const onMouseDown = (e) => {
        isResizing = true;
        startX = e.clientX;
        const styles = window.getComputedStyle(col);
        startLeftWidth = parseInt(styles.width, 10);
        resizer.classList.add('resizing');
        document.body.style.cursor = 'col-resize';
        e.preventDefault();
    };

    const onMouseMove = (e) => {
        if (!isResizing) return;
        const dx = e.clientX - startX;
        col.style.width = `${startLeftWidth + dx}px`;
    };

    const onMouseUp = async () => {
        if (isResizing) {
            isResizing = false;
            resizer.classList.remove('resizing');
            document.body.style.cursor = '';
            await dotNetHelper.invokeMethodAsync("SetColumnWidthAsync", col.id, col.style.width);
        }
    };
    
    resizer.addEventListener('mousedown', onMouseDown);
    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);

    return {
        dispose: () => {
            resizer.removeEventListener('mousedown', onMouseDown);
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
        }
    };
}
async function downloadFileFromStream(fileName, contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}

// Extensions for BlazorMonaco to get diff navigation.
var navi;

function createDiffNavigator(id, options) {
    let editor = blazorMonaco.editor.getEditor(id);
    navi = monaco.editor.createDiffNavigator(editor, options);
}

function diffNavigationNext() {
    navi.next();
}

function diffNavigationPrevious() {
    navi.previous();
}
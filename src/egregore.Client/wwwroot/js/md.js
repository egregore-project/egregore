
function initMarkdownEditor() {
    easyMDE = new EasyMDE({element: $('#mde')[0]});    
}

function getMarkdownEditorContent() {
    var md = easyMDE.value();
    return md;
}
// ReSharper disable PossiblyUnassignedProperty
// ReSharper disable UseOfImplicitGlobalInFunctionScope
$(document).ready(function() {

    const originalText = "Copy to clipboard";
    const failureText = "Copy with Ctrl+C";
    const successText = "Copied!";
    
    const btn = $("#copy-button");

    btn.tooltip({ placement: "top" });
    btn.bind("click", function() {
        const input = document.querySelector("#challenge");
        input.setSelectionRange(0, input.value.length + 1);
        try {
            const success = document.execCommand("copy");
            if (success) {
                btn.trigger("copied", [successText]);
            } else {
                btn.trigger("copied", [failureText]);
            }
        } catch (err) {
            btn.trigger("copied", [failureText]);
        }
    });
    btn.bind("copied", function(_, message) {
        $(this).attr("title", message)
            .tooltip("_fixTitle")
            .tooltip("show").attr("title", originalText)
            .tooltip("_fixTitle");
    });
});
// Used for ValidateCount on array parameters (multi-select listboxes and multiline textareas).
// Counts the number of selected/filled items and validates the count is within the
// allowed range defined by data-min and data-max on the validator element.
function validateCollection(sender, args) {
    var controlobj = document.getElementById(sender.controltovalidate);
    var selecteditems = 0
    if (controlobj.tagName.toUpperCase() == 'SELECT') {
        // For a listbox, count options that are selected and not the --Select-- placeholder
        var options = controlobj.options;
        for (var i = 0; i < options.length; i++) {
            if (options[i].selected == true && options[i].value != '--Select--') {
                selecteditems++
            }
        }
    } else if (controlobj.tagName.toUpperCase() == 'TEXTAREA') {
        // For a textarea, count non-blank lines
        var inputarr = controlobj.value.split("\n")
        for (var i = 0; i < inputarr.length; i++) {
            if (inputarr[i].trim() != "") {
                selecteditems++
            }
        }
    }

    if (selecteditems >= sender.dataset.min && selecteditems <= sender.dataset.max) {
        args.IsValid = true;
    } else {
        args.IsValid = false;
    }
}

// Used for ValidateRange on array parameters (e.g. [int[]], [float[]]).
// Each non-blank line of the textarea is validated independently against the
// allowed range defined by data-min and data-max on the validator element.
// data-type controls whether lines are parsed as integers or floats.
// Fails immediately on the first line that is out of range or not a valid number.
function validateRangeMultiline(sender, args) {
    var min = parseFloat(sender.dataset.min);
    var max = parseFloat(sender.dataset.max);
    var isFloat = sender.dataset.type === 'float';
    // Use args.Value (provided by ASP.NET) rather than getElementById to avoid
    // issues with ASP.NET-mangled control IDs for dynamically added controls.
    var lines = args.Value.split("\n");
    for (var i = 0; i < lines.length; i++) {
        var trimmed = lines[i].trim();
        if (trimmed === '') continue; // skip blank lines
        var val = isFloat ? parseFloat(trimmed) : parseInt(trimmed, 10);
        if (isNaN(val) || val < min || val > max) {
            args.IsValid = false;
            return;
        }
    }
    args.IsValid = true;
}

// Used for Mandatory validation on boolean/checkbox parameters.
// ASP.NET does not support pointing a CustomValidator directly at a checkbox,
// so the checkbox ID is stored in data-control and looked up manually.
function validateMandatoryCheckbox(sender, args) {
    var controlobj = document.getElementById(sender.dataset.control);

    if (controlobj.checked) {
        args.IsValid = true;
    } else {
        args.IsValid = false;
    }
}

// Attached to the Submit button's OnClientClick handler to prevent double-submission.
// Runs all ASP.NET client-side validators first; if any fail the submit is cancelled.
// On success, shows the loading spinner and disables all inputs to block re-submission
// while the postback is in progress.
function disableOnPostback() {

    // Cancel the submit if any validator fails
    try {
        if (!Page_ClientValidate()) { return false; }
    } catch (ex) { }

    // Show the loading spinner and disable inputs after a short delay to allow
    // the form submission to begin before the inputs are locked
    $("#imgLoader").show();
    window.setTimeout(function () { $(":input").attr("disabled", true); }, 10);
    return true;
}
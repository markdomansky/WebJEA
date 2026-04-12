//validation script for listbox
function validateCollection(sender, args) {
    //console.log(sender);
    //console.log(args);
    var controlobj = document.getElementById(sender.controltovalidate);
    var selecteditems = 0
    if (controlobj.tagName.toUpperCase() == 'SELECT') {
        //dropdown
        var options = controlobj.options;
        for (var i = 0; i < options.length; i++) { //skip the first item because it's the --select-- value
            if (options[i].selected == true && options[i].value != '--Select--') {
                console.log("seli ++");
                selecteditems++
            }
        }
    } else if (controlobj.tagName.toUpperCase() == 'TEXTAREA') {
        //textarea, multiline
        var inputarr = controlobj.value.split("\n")
        for (var i = 0; i < inputarr.length; i++) { //skip the first item because it's the --select-- value
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

function validateMandatoryCheckbox(sender, args) {
    //console.log(sender);
    //console.log(args);
    var controlobj = document.getElementById(sender.dataset.control);

    if (controlobj.checked) {
        args.IsValid = true;
    } else {
        args.IsValid = false;
    }
}

function disableOnPostback() {

    //check for validation
    try {
        if (!Page_ClientValidate()) { return false; }
    } catch (ex) { }

    //disable inputs, show loader
    $("#imgLoader").show();
    window.setTimeout('$(":input").attr("disabled", true);', 10);
    return true;
}
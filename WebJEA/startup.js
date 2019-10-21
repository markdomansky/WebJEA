//check if there's a data-type
//data-type=date //date only
$('*[data-type="date"]').datepicker({ dateFormat:"yy/mm/dd"});

//date-type=datetime //date and time
$('*[data-type="datetime"]').datetimepicker({ dateFormat: "yy/mm/dd" });

//disables inputs on submit.
//window.onbeforeunload = disableOnPostback;
//side-effect, even affects changing between cmds
//$("#btnRun").on('click',disableOnPostback());


//this makes the li element clickable using the A elements link
$(function () {
    $('li.menulink').click(function () {
        window.location = $('a', this).attr('href');
        return false;
    });
});

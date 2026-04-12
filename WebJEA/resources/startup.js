//check if there's a data-type
//data-type=date //date only
$('*[data-type="date"]').datepicker({ dateFormat:"yy/mm/dd"});

//date-type=datetime //date and time
$('*[data-type="datetime"]').datetimepicker({ dateFormat: "yy/mm/dd" });

//this makes the li element clickable using the A elements link
$(function () {
    $('li.menulink').click(function () {
        window.location = $('a', this).attr('href');
        return false;
    });

    // Clone desktop sidebar menu into the mobile offcanvas drawer
    var desktopMenu = document.querySelector('#desktopSidebar .sidebar-menu-list');
    var offcanvasTarget = document.getElementById('offcanvasMenuTarget');
    var offcanvasTitle = document.getElementById('offcanvasTitle');
    var offcanvasFooter = document.getElementById('offcanvasFooter');
    var sidebarBrand = document.querySelector('.sidebar-brand .brand');
    var sidebarFooter = document.querySelector('.sidebar-footer .footer');

    if (desktopMenu && offcanvasTarget) {
        offcanvasTarget.innerHTML = desktopMenu.innerHTML;
        // Make offcanvas menu items navigable
        $(offcanvasTarget).find('li.menulink').click(function () {
            window.location = $('a', this).attr('href');
            return false;
        });
    }
    if (sidebarBrand && offcanvasTitle) {
        offcanvasTitle.textContent = sidebarBrand.textContent;
    }
    if (sidebarFooter && offcanvasFooter) {
        offcanvasFooter.innerHTML = sidebarFooter.innerHTML;
    }
});

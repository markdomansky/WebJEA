/* command-init.js
   Initializes command page without inline scripts.
   Reads `data-cmdid` from the script tag and calls webjeaInit().
*/
(function () {
    var script = document.currentScript || (function () {
        var s = document.getElementsByTagName('script');
        return s[s.length - 1];
    })();
    var cmd = script ? script.getAttribute('data-cmdid') : null;
    if (cmd) {
        window.webjeaCmdId = cmd;
    }
    if (typeof webjeaInit === 'function') {
        try {
            webjeaInit();
        } catch (err) {
            console.error('webjeaInit error', err);
        }
    }
})();

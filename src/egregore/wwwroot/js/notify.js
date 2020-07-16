"use strict";

var connection = new signalR.HubConnectionBuilder()
    .withUrl("/notify")
    .withAutomaticReconnect([0, 1000, 5000, null])
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("ReceiveMessage", function (level, message) {
    var encoded = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    toastr[level](encoded);
});

connection.start().then(function () {
}).catch(function (err) {
    return console.error(err.toString());
});

document.addEventListener("DOMContentLoaded", function(event) { 
    toastr.options = {
        "closeButton": false,
        "debug": false,
        "newestOnTop": true,
        "progressBar": true,
        "positionClass": "toast-bottom-right",
        "preventDuplicates": false,
        "onclick": null,
        "showDuration": "300",
        "hideDuration": "1000",
        "timeOut": "5000",
        "extendedTimeOut": "1000",
        "showEasing": "swing",
        "hideEasing": "linear",
        "showMethod": "fadeIn",
        "hideMethod": "fadeOut"
    }
});
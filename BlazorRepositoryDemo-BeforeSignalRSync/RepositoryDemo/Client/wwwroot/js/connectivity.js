let notify;

window.connectivity = {
    initialize: function (interop) {

        notify = function () {
            interop.invokeMethodAsync("ConnectivityChanged", navigator.onLine);
        }

        window.addEventListener("online", notify);
        window.addEventListener("offline", notify);

        notify(navigator.onLine);
    },
    dispose: function () {

        if (handler != null) {

            window.removeEventListener("online", notify);
            window.removeEventListener("offline", notify);
        }
    }
};
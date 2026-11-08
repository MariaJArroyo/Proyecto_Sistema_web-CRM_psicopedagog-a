// Gancho unico para el 401 del panel.
//
// Las pantallas del panel piden sus datos con fetch a /Admin/..., y ese
// controlador reenvia el codigo del API tal cual. Si el token que viaja dentro
// de la cookie ya no sirve, llega un 401 y antes de esto cada pantalla lo
// mostraba como un "no se pudo" cualquiera, igual que si el API estuviera
// apagado. Son dos cosas distintas: del API caido se sale esperando, de la
// sesion vencida se sale volviendo a entrar.
//
// Se envuelve fetch una sola vez en vez de tocar los 24 archivos de JavaScript.
// Va cargado antes que el resto de los scripts del panel para que ya este puesto
// cuando alguno pida datos al arrancar la pantalla.
//
// El portal tiene su propio trato del 401 en portalComun.js y no carga este
// archivo: ahi la pantalla avisa y no redirige.
(function () {
    "use strict";

    var RUTA_SESION_VENCIDA = "/Cuenta/SesionVencida";

    // Una sola redireccion: si dos peticiones fallan juntas, no se pelean por
    // mover la pagina
    var yendoAlLogin = false;

    var fetchOriginal = window.fetch;

    if (typeof fetchOriginal !== "function") {
        return;
    }

    function esDeLaCasa(url) {
        try {
            return new URL(url, window.location.origin).origin === window.location.origin;
        } catch {
            return false;
        }
    }

    window.fetch = async function (recurso, opciones) {
        var respuesta = await fetchOriginal.call(window, recurso, opciones);

        if (respuesta.status !== 401 || yendoAlLogin) {
            return respuesta;
        }

        // respuesta.url viene vacia en algunos casos (respuestas opacas), y ahi
        // sirve lo que se pidio
        var url = respuesta.url
            || (typeof recurso === "string" ? recurso : recurso && recurso.url);

        if (!esDeLaCasa(url)) {
            return respuesta;
        }

        yendoAlLogin = true;

        window.location.assign(RUTA_SESION_VENCIDA);

        return respuesta;
    };
})();

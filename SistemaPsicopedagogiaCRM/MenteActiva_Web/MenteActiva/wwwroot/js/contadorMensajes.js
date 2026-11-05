// Contador de mensajes internos sin leer. El mismo archivo sirve en el portal y
// en el panel: cada layout pone su burbuja con data-contador-mensajes y ahi
// dentro va la ruta que le toca consultar, porque el proxy es distinto en cada
// lado. Asi no hay dos copias del mismo contador.
// Molde: contadorSolicitudes.js, que hace lo mismo con las solicitudes.
(function () {

    const contador = document.querySelector("[data-contador-mensajes]");
    if (!contador) return;

    const ruta = contador.getAttribute("data-contador-mensajes");
    if (!ruta) return;

    let anterior = null;

    async function actualizar() {
        try {
            const respuesta = await fetch(ruta, {
                headers: { "Accept": "application/json" }
            });

            if (!respuesta.ok) return;

            const { sinLeer } = await respuesta.json();

            contador.textContent = sinLeer > 99 ? "99+" : sinLeer;
            contador.classList.toggle("d-none", sinLeer === 0);
            contador.title = sinLeer === 1
                ? "1 mensaje sin leer"
                : `${sinLeer} mensajes sin leer`;

            // Si llegaron nuevos desde la ultima consulta, la burbuja "late"
            if (anterior !== null && sinLeer > anterior) {
                contador.classList.remove("pulso");
                void contador.offsetWidth;   // reinicia la animacion
                contador.classList.add("pulso");
            }

            anterior = sinLeer;

        } catch {
            // Si el API no responde, el contador se queda como estaba: es un
            // adorno y no vale la pena molestar a la persona con un error
        }
    }

    // La pantalla de mensajeria lo llama despues de abrir un hilo, que es
    // justo cuando lo que habia sin leer deja de estarlo
    window.actualizarContadorMensajes = actualizar;

    actualizar();
    setInterval(actualizar, 60000);
})();

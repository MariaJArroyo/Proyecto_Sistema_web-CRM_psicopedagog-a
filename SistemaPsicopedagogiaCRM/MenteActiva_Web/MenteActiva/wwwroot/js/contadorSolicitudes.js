// Contador de solicitudes pendientes en el menu del panel.
// Se carga en todas las pantallas desde _LayoutAdmin y se refresca cada minuto.
(function () {

    const contador = document.getElementById("ContadorSolicitudes");
    if (!contador) return;

    let anterior = null;

    async function actualizar() {
        try {
            const respuesta = await fetch("/Admin/ContarSolicitudesPendientes", {
                headers: { "Accept": "application/json" }
            });

            if (!respuesta.ok) return;

            const { pendientes } = await respuesta.json();

            contador.textContent = pendientes > 99 ? "99+" : pendientes;
            contador.classList.toggle("d-none", pendientes === 0);
            contador.title = pendientes === 1
                ? "1 solicitud pendiente"
                : `${pendientes} solicitudes pendientes`;

            // Si llegaron nuevas desde la ultima consulta, el circulo "late"
            if (anterior !== null && pendientes > anterior) {
                contador.classList.remove("pulso");
                void contador.offsetWidth;   // reinicia la animacion
                contador.classList.add("pulso");
            }

            anterior = pendientes;

        } catch {
            // Sesion vencida o sin conexion: se deja el contador como estaba
        }
    }

    // La pantalla de Solicitudes lo llama despues de cada cambio
    window.actualizarContadorSolicitudes = actualizar;

    actualizar();
    setInterval(actualizar, 60000);

})();
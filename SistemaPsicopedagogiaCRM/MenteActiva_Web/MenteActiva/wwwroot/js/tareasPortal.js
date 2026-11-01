(function () {
    "use strict";
    const comun = window.PortalComun;
    const pantalla = comun.iniciarPantalla("PortalTareas");
    if (!pantalla) return;
    const alerta = document.getElementById("MensajeDescargaTarea");
    pantalla.addEventListener("click", async evento => {
        const boton = evento.target.closest("[data-descargar-tarea]");
        if (!boton || boton.disabled) return;
        const texto = boton.textContent;
        boton.disabled = true;
        boton.setAttribute("aria-busy", "true");
        boton.textContent = "Descargando…";
        comun.avisar(alerta, "");
        try {
            await comun.descargarArchivo(boton.dataset.url);
        } catch (error) {
            comun.avisar(alerta, error.message || "No se pudo descargar el archivo.");
        } finally {
            boton.disabled = false;
            boton.removeAttribute("aria-busy");
            boton.textContent = texto;
        }
    });
})();
